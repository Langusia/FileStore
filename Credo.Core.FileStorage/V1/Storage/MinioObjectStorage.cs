using Credo.Core.FileStorage.V1.DB.Models;
using Credo.Core.FileStorage.V1.DB.Models.Upload;
using Credo.Core.FileStorage.V1.DB.Repositories;
using Credo.Core.FileStorage.V1.Entities;
using Credo.Core.FileStorage.V1.Validation;
using Minio;
using Minio.DataModel.Args;

namespace Credo.Core.FileStorage.V1.Storage;

public sealed class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient _minio;
    private readonly IChannelOperationBucketRepository _cobRepo;
    private readonly IDocumentsRepository _docs;

    public MinioObjectStorage(IMinioClient minio, IChannelOperationBucketRepository cobRepo, IDocumentsRepository docs)
        => (_minio, _cobRepo, _docs) = (minio, cobRepo, docs);

    // --- New, preferred API ---
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

        short typeCode;
        try
        {
            // Strict allow-list validation (pdf/csv/xls/xlsx/jpg/png etc.)
            var tv = await FileTypeValidator.ValidateOrThrowAsync(seekable, safeFileName, file.ContentType, ct);
            typeCode = tv.TypeCode;
        }
        finally
        {
            if (seekable.CanSeek) seekable.Position = 0;
        }

        // 3) Preferred MIME + extension based on authoritative type code
        var preferredMime = DocumentTypeCodes.ToContentType(typeCode);
        var nowUtc = DateTime.UtcNow;
        var ext = PreferredExtensionForMime(preferredMime)
                  ?? Path.GetExtension(safeFileName).TrimStart('.').ToLowerInvariant();
        var objKey = BuildObjectKey(options?.ObjectKeyPrefix, ext, nowUtc);
        var docName = string.IsNullOrWhiteSpace(options?.LogicalName) ? safeFileName : options!.LogicalName!;

        // 4) Ensure bucket exists after validation
        await EnsureBucketAsync(bucketName, ct);

        // 5) Upload first; compensate S3 on DB failure
        try
        {
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName.ToLower())
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

        // 6) DB row
        Guid docId;
        try
        {
            docId = await _docs.InsertAsync(
                new DocumentCreate(
                    ChannelOperationBucketId: routeId,
                    Name: docName,
                    Address: objKey,
                    Size: size,
                    Type: typeCode),
                ct);
        }
        catch
        {
            try
            {
                await _minio.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucketName).WithObject(objKey), ct);
            }
            catch
            {
            }

            throw;
        }

        return new UploadResult(docId, bucketName, objKey, docName, size, typeCode, nowUtc);
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

    // ---------- route resolution via repo ----------
    private Task<ChannelOperationBucket> ResolveRouteAsync(IUploadRouteArgs route, CancellationToken ct) =>
        route switch
        {
            AliasArgs a => _cobRepo.GetByAliasesAsync(a.ChannelAlias, a.OperationAlias, ct),
            ExternalAliasArgs ea => _cobRepo.GetByExternalAliasesAsync(ea.ChannelExternalAlias, ea.OperationExternalAlias, ct),
            ExternalIdArgs ei => _cobRepo.GetByExternalIdsAsync(ei.ChannelExternalId, ei.OperationExternalId, ct),
            ChOpBucketArgs id => _cobRepo.GetByIdAsync(id.ChannelOperationBucketId, ct),
            BucketNameArgs bn => _cobRepo.GetOrCreateDefaultForBucketAsync(bn.BucketName, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };

    // ---------- helpers ----------
    private async Task EnsureBucketAsync(string bucket, CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket.ToLower()), ct);
        if (!exists) await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket.ToLower()), ct);
    }

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