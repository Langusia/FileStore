using Credo.Core.FileStorage.DB.Entities;
using Credo.Core.FileStorage.DB.Repositories;
using Credo.Core.FileStorage.Models.Download;
using Credo.Core.FileStorage.Models.Upload;
using Credo.Core.FileStorage.Validation;
using Minio;
using Minio.DataModel.Args;

namespace Credo.Core.FileStorage.Storage;

public sealed class MinioObjectStorage(IMinioClient minio, IChannelOperationBucketRepository cobRepo, IDocumentsRepository docs, IFileTypeInspector inspector)
    : IObjectStorage
{
    public async Task<StorageObject> OpenReadAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await docs.TryGetAsync(documentId, ct)
                  ?? throw new FileNotFoundException($"Document {documentId} not found");

        return await OpenReadCoreAsync(doc.ChannelOperationBucket.Bucket.Name,
            doc.Key, doc.Size, doc.Type, doc.Name, ct);
    }

    public async Task<StorageObject> OpenReadAsync(string bucket, string objectKey, CancellationToken ct = default)
    {
        var doc = await docs.TryGetAsync(bucket, objectKey, ct)
                  ?? throw new FileNotFoundException($"Document {bucket}/{objectKey} not found");

        return await OpenReadCoreAsync(bucket, objectKey, doc.Size, doc.Type, doc.Name, ct);
    }

    // public async Task<StorageObject> OpenReadAsync(string bucket, string objectKey, CancellationToken ct = default)
    // {
    //     var ms = new MemoryStream();
    //     var args = new GetObjectArgs()
    //         .WithBucket(bucket)
    //         .WithObject(objectKey)
    //         .WithCallbackStream(src => src.CopyTo(ms));
    //
    //     await minio.GetObjectAsync(args, ct).ConfigureAwait(false);
    //     ms.Position = 0;
    //
    //     var ext = Path.GetExtension(objectKey).TrimStart('.').ToLowerInvariant();
    //     var contentType = ext switch
    //     {
    //         "pdf" => "application/pdf",
    //         "png" => "image/png",
    //         "jpg" or "jpeg" => "image/jpeg",
    //         "csv" => "text/csv",
    //         "zip" => "application/zip",
    //         "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    //         _ => "application/octet-stream"
    //     };
    //
    //     return new StorageObject(contentType, Path.GetFileName(objectKey), ms);
    // }

    private async Task<StorageObject> OpenReadCoreAsync(
        string bucket,
        string objectKey,
        long size,
        short typeCode,
        string fileName,
        CancellationToken ct)
    {
        // Pre-size buffer if size is known and < 2GB
        var ms = (size > 0 && size <= int.MaxValue)
            ? new MemoryStream(capacity: (int)size)
            : new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(bucket.ToLowerInvariant())
            .WithObject(objectKey)
            .WithCallbackStream(src => src.CopyTo(ms));

        await minio.GetObjectAsync(args, ct).ConfigureAwait(false);
        ms.Position = 0;

        return new StorageObject(MimeMap.ToContentType(typeCode),
            string.IsNullOrWhiteSpace(fileName)
                ? Path.GetFileName(objectKey)
                : fileName, ms);
    }

    public async Task<UploadResult> Upload(
        IUploadRouteArgs route,
        UploadFile file,
        UploadOptions? options = null,
        CancellationToken ct = default)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));
        if (file.Content is null) throw new ArgumentException("UploadFile.Content is required");
        if (string.IsNullOrWhiteSpace(file.FileName)) throw new ArgumentException("UploadFile.FileName is required");

        // 1) Resolve route -> bucket
        var cob = await ResolveRouteAsync(route, ct);
        var bucketName = cob.Bucket.Name;
        var routeId = cob.Id;

        // 2) Make stream seekable + determine size
        var safeFileName = Path.GetFileName(file.FileName);
        var (seekable, size, isTemp) = await EnsureSeekableAsync(file.Content, ct);

        // 3) Detect type (binary via Mime-Detective; CSV via CsvHelper probe)
        short typeCode;
        try
        {
            typeCode = await inspector.DetectOrThrowAsync(seekable, safeFileName, file.ContentType, ct);
        }
        finally
        {
            if (seekable.CanSeek) seekable.Position = 0;
        }

        // 4) Preferred MIME + extension from your map
        var preferredMime = MimeMap.ToContentType(typeCode);
        var nowUtc = DateTime.UtcNow;
        var ext = MimeMap.PreferredExtensionForMime(preferredMime)
                  ?? Path.GetExtension(safeFileName).TrimStart('.').ToLowerInvariant();

        var objKey = BuildObjectKey(options?.ObjectKeyPrefix, ext, nowUtc);
        var docName = string.IsNullOrWhiteSpace(options?.LogicalName) ? safeFileName : options!.LogicalName!;

        // 5) Ensure bucket exists (after validation)
        await EnsureBucketAsync(bucketName, ct);

        // 6) Upload to MinIO first; if DB fails we compensate by deleting the object
        try
        {
            await minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName.ToLowerInvariant())
                .WithObject(objKey)
                .WithStreamData(seekable)
                .WithObjectSize(size)
                .WithContentType(preferredMime), ct);
        }
        finally
        {
            // Always clean temp; optionally dispose caller’s stream
            if (isTemp) await seekable.DisposeAsync();
            if (file.DisposeStream) await file.DisposeAsync();
        }

        // 7) Write DB row; compensate S3 on DB failure
        Guid docId;
        try
        {
            docId = await docs.InsertAsync(
                new DocumentCreate(
                    ChannelOperationBucketId: routeId,
                    Name: docName,
                    Address: $"{bucketName}/{objKey}",
                    objKey,
                    Size: size,
                    Type: typeCode),
                ct);
        }
        catch
        {
            try
            {
                await minio.RemoveObjectAsync(
                    new RemoveObjectArgs()
                        .WithBucket(bucketName.ToLowerInvariant())
                        .WithObject(objKey), ct);
            }
            catch
            {
                // swallow compensation failure; original exception will bubble
            }

            throw;
        }

        return new UploadResult(docId, bucketName, objKey, docName, $"{bucketName}/{objKey}", size, typeCode, nowUtc);
    }


    // --- Back-compat shim: keep old signature pointing to the new API ---
    public Task<UploadResult> Upload(
        IUploadRouteArgs route,
        Stream content,
        string fileName,
        string? contentType = null,
        UploadOptions? options = null,
        CancellationToken ct = default)
        => Upload(route, UploadFile.FromStream(content, fileName, contentType, declaredLength: null, disposeStream: false), options, ct);

    // --- New interface methods for MigrationRunner ---
    public async Task EnsureBucketAsync(string bucketName, CancellationToken ct = default)
    {
        var exists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName.ToLowerInvariant()), ct);
        if (!exists) 
        {
            await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName.ToLowerInvariant()), ct);
        }
    }

    public async Task PutAsync(string bucketName, string objectKey, Stream data, long size, string contentType, CancellationToken ct = default)
    {
        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName.ToLowerInvariant())
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType), ct);
    }

    // ---------- route resolution via repo ----------
    private Task<ChannelOperationBucket> ResolveRouteAsync(IUploadRouteArgs route, CancellationToken ct) =>
        route switch
        {
            AliasArgs a => cobRepo.GetByAliasesAsync(a.ChannelAlias, a.OperationAlias, ct),
            ExternalAliasArgs ea => cobRepo.GetByExternalAliasesAsync(ea.ChannelExternalAlias, ea.OperationExternalAlias, ct),
            ExternalIdArgs ei => cobRepo.GetByExternalIdsAsync(ei.ChannelExternalId, ei.OperationExternalId, ct),
            ChOpBucketArgs id => cobRepo.GetByIdAsync(id.ChannelOperationBucketId, ct),
            BucketNameArgs bn => cobRepo.GetOrCreateDefaultForBucketAsync(bn.BucketName, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };

    // ---------- helpers ----------

    private static string BuildObjectKey(string? prefix, string ext, DateTime nowUtc)
    {
        var pre = string.IsNullOrWhiteSpace(prefix) ? $"{nowUtc:yyyy}-{nowUtc:MM}-{nowUtc:dd}" : prefix!.Trim().Trim('/');
        var id = Guid.NewGuid().ToString("N");
        return string.IsNullOrEmpty(ext) ? $"{pre}-{id}" : $"{pre}-{id}.{ext}";
    }

    private static string? PreferredExtensionForMime(string mime) => mime.ToLowerInvariant() switch
    {
        "application/pdf" => "pdf",
        "text/csv" => "csv",
        "application/vnd.ms-excel" => "xls",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "xlsx",
        "image/jpeg" => "jpg",
        "image/png" => "png",
        _ => null
    };

    private static async Task<(Stream stream, long size, bool isTempFile)> EnsureSeekableAsync(Stream input, CancellationToken ct)
    {
        if (input.CanSeek)
        {
            if (input.Position != 0) input.Position = 0;
            return (input, input.Length, false);
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"upload_{Guid.NewGuid():N}.tmp");
        var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
            bufferSize: 1024 * 64,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

        await input.CopyToAsync(fs, ct);
        fs.Position = 0;
        return (fs, fs.Length, true);
    }
}