using Credo.Core.FileStorage.DB.Entities;
using Credo.Core.FileStorage.DB.Repositories;
using Credo.Core.FileStorage.Models.Download;
using Credo.Core.FileStorage.Models.Upload;
using Credo.Core.FileStorage.Validation;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Collections.Concurrent;

namespace Credo.Core.FileStorage.Storage;

public sealed class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient _minio;
    private readonly IChannelOperationBucketRepository _cobRepo;
    private readonly IDocumentsRepository _docs;
    private readonly UploadService _uploadPipeline;
    private readonly ConcurrentDictionary<string, bool> _bucketCache = new();

    public MinioObjectStorage(
        IMinioClient minio,
        IChannelOperationBucketRepository cobRepo,
        IDocumentsRepository docs,
        IFileTypeInspector inspector)
    {
        _minio = minio;
        _cobRepo = cobRepo;
        _docs = docs;
        _uploadPipeline = new UploadService(minio, docs, inspector);
    }

    // ============================================================================
    // READ OPERATIONS
    // ============================================================================

    public async Task<StorageObject> OpenReadAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _docs.TryGetAsync(documentId, ct)
                  ?? throw new FileNotFoundException($"Document {documentId} not found");

        return await OpenReadCoreAsync(
            doc.ChannelOperationBucket.Bucket.Name,
            doc.Key,
            doc.Size,
            doc.Type,
            doc.Name,
            ct);
    }

    public async Task<StorageObject> OpenReadAsync(string bucket, string objectKey, CancellationToken ct = default)
    {
        var doc = await _docs.TryGetAsync(bucket, objectKey, ct)
                  ?? throw new FileNotFoundException($"Document {bucket}/{objectKey} not found");

        return await OpenReadCoreAsync(bucket, objectKey, doc.Size, doc.Type, doc.Name, ct);
    }

    private async Task<StorageObject> OpenReadCoreAsync(
        string bucket,
        string objectKey,
        long size,
        short typeCode,
        string fileName,
        CancellationToken ct)
    {
        var ms = StreamHelper.CreateMemoryStream(size);

        var args = new GetObjectArgs()
            .WithBucket(bucket.ToLowerInvariant())
            .WithObject(objectKey)
            .WithCallbackStream(src => src.CopyTo(ms));

        await _minio.GetObjectAsync(args, ct).ConfigureAwait(false);
        ms.Position = 0;

        return new StorageObject(
            MimeMap.ToContentType(typeCode),
            string.IsNullOrWhiteSpace(fileName)
                ? Path.GetFileName(objectKey)
                : fileName,
            ms);
    }

    // ============================================================================
    // UPLOAD OPERATIONS (HIGH-LEVEL)
    // ============================================================================

    public async Task<UploadResult> UploadAsync(
        IUploadRouteArgs route,
        UploadFile file,
        UploadOptions? options = null,
        CancellationToken ct = default)
    {
        UploadService.ValidateRequest(file);

        // 1) Resolve route to bucket
        var cob = await ResolveRouteAsync(route, ct);

        // 2) Prepare upload context
        var context = await _uploadPipeline.PrepareContextAsync(file, cob, options, ct);

        // 3) Ensure bucket exists
        await EnsureBucketExistsAsync(context.BucketName, ct);

        // 4) Upload to storage
        await _uploadPipeline.UploadToStorageAsync(context, ct);

        // 5) Record in database
        var docId = await _uploadPipeline.RecordInDatabaseAsync(context, ct);

        // 6) Return result
        return UploadService.CreateResult(docId, context);
    }

    public Task<UploadResult> UploadAsync(
        IUploadRouteArgs route,
        Stream content,
        string fileName,
        string? contentType = null,
        UploadOptions? options = null,
        CancellationToken ct = default)
        => UploadAsync(
            route,
            UploadFile.FromStream(content, fileName, contentType, declaredLength: null, disposeStream: false),
            options,
            ct);

    // ============================================================================
    // UPLOAD OPERATIONS (LOW-LEVEL)
    // ============================================================================

    public async Task PutObjectAsync(
        string bucketName,
        string objectKey,
        Stream data,
        long size,
        string contentType,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(data);

        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Size cannot be negative");

        if (!data.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(data));

        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName.ToLowerInvariant())
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType ?? "application/octet-stream"), ct);
    }

    // ============================================================================
    // BUCKET OPERATIONS
    // ============================================================================

    public async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);

        var normalizedName = bucketName.ToLowerInvariant();

        if (_bucketCache.ContainsKey(normalizedName))
            return;

        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(normalizedName), ct);

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(normalizedName), ct);
        }

        _bucketCache.TryAdd(normalizedName, true);
    }

    // ============================================================================
    // OBJECT EXISTENCE & METADATA OPERATIONS
    // ============================================================================

    public async Task<bool> ObjectExistsAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucketName.ToLowerInvariant())
                .WithObject(objectKey);

            await _minio.StatObjectAsync(args, ct);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    public async Task<ObjectMetadata?> GetObjectMetadataAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucketName.ToLowerInvariant())
                .WithObject(objectKey);

            var stat = await _minio.StatObjectAsync(args, ct);

            return new ObjectMetadata(
                Bucket: bucketName,
                ObjectKey: objectKey,
                Size: stat.Size,
                ContentType: stat.ContentType,
                ETag: stat.ETag,
                LastModified: stat.LastModified.ToUniversalTime()
            );
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            return null;
        }
    }

    // ============================================================================
    // ROUTE RESOLUTION
    // ============================================================================

    private Task<ChannelOperationBucket> ResolveRouteAsync(IUploadRouteArgs route, CancellationToken ct) =>
        route switch
        {
            AliasArgs a => _cobRepo.GetByAliasesAsync(a.ChannelAlias, a.OperationAlias, ct),
            ExternalAliasArgs ea => _cobRepo.GetByExternalAliasesAsync(ea.ChannelExternalAlias, ea.OperationExternalAlias, ct),
            ExternalIdArgs ei => _cobRepo.GetByExternalIdsAsync(ei.ChannelExternalId, ei.OperationExternalId, ct),
            ChOpBucketArgs id => _cobRepo.GetByIdAsync(id.ChannelOperationBucketId, ct),
            BucketNameArgs bn => _cobRepo.GetOrCreateDefaultForBucketAsync(bn.BucketName, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(route), $"Unsupported route type: {route.GetType().Name}")
        };
}