using System.Security.Cryptography;
using System.Text.Json;
using FileStore.Core.Enums;
using FileStore.Core.Exceptions;
using FileStore.Core.Interfaces;
using FileStore.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileStore.Infrastructure.Services;

public class StorageService : IStorageService
{
    private readonly IFileStorageBackend _backend;
    private readonly IObjectRepository _objectRepository;
    private readonly IObjectLinkRepository _linkRepository;
    private readonly IShardingStrategy _shardingStrategy;
    private readonly StorageServiceOptions _options;
    private readonly ILogger<StorageService> _logger;

    public StorageService(
        IFileStorageBackend backend,
        IObjectRepository objectRepository,
        IObjectLinkRepository linkRepository,
        IShardingStrategy shardingStrategy,
        IOptions<StorageServiceOptions> options,
        ILogger<StorageService> logger)
    {
        _backend = backend;
        _objectRepository = objectRepository;
        _linkRepository = linkRepository;
        _shardingStrategy = shardingStrategy;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UploadResult> UploadAsync(UploadRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Channel))
            throw new ArgumentException("Channel is required", nameof(request.Channel));

        if (string.IsNullOrWhiteSpace(request.Operation))
            throw new ArgumentException("Operation is required", nameof(request.Operation));

        ValidateContentType(request.ContentType);

        var objectId = Guid.NewGuid();
        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrEmpty(extension))
            extension = ".bin";

        var relativePath = _shardingStrategy.ComputeRelativePath(objectId, extension, _options.Shard);
        var tier = StorageTier.Hot;

        string hash;
        long size;

        using (var hashStream = new MemoryStream())
        {
            await request.FileStream.CopyToAsync(hashStream, cancellationToken);
            size = hashStream.Length;

            if (_options.MaxFileSizeMb > 0 && size > _options.MaxFileSizeMb * 1024 * 1024)
            {
                throw new StorageException($"File size exceeds maximum allowed size of {_options.MaxFileSizeMb}MB");
            }

            hashStream.Position = 0;
            hash = await ComputeHashAsync(hashStream, cancellationToken);

            hashStream.Position = 0;
            await _backend.StoreAsync(hashStream, relativePath, tier, cancellationToken);
        }

        var storedObject = new StoredObject
        {
            ObjectId = objectId,
            Bucket = request.Bucket,
            RelativePath = relativePath,
            Tier = tier,
            Length = size,
            ContentType = request.ContentType,
            Hash = hash,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = null,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null
        };

        await _objectRepository.CreateAsync(storedObject, cancellationToken);

        var link = new ObjectLink
        {
            ObjectId = objectId,
            Channel = request.Channel,
            Operation = request.Operation,
            BusinessEntityId = request.BusinessEntityId,
            CreatedAt = DateTime.UtcNow
        };

        await _linkRepository.CreateAsync(link, cancellationToken);

        _logger.LogInformation("Uploaded object {ObjectId} to bucket {Bucket}, size {Size} bytes", objectId, request.Bucket, size);

        return new UploadResult
        {
            ObjectId = objectId,
            Bucket = request.Bucket,
            Size = size,
            Hash = hash,
            ContentType = request.ContentType,
            CreatedAt = storedObject.CreatedAt
        };
    }

    public async Task<Stream> DownloadAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default)
    {
        var obj = await _objectRepository.GetByBucketAndIdAsync(bucket, objectId, cancellationToken);
        if (obj == null)
            throw new ObjectNotFoundException(bucket, objectId);

        var stream = await _backend.RetrieveAsync(obj.RelativePath, obj.Tier, cancellationToken);

        await _objectRepository.UpdateLastAccessedAsync(objectId, DateTime.UtcNow, cancellationToken);

        _logger.LogInformation("Downloaded object {ObjectId} from bucket {Bucket}", objectId, bucket);

        return stream;
    }

    public async Task<ObjectMetadata?> GetMetadataAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default)
    {
        var obj = await _objectRepository.GetByBucketAndIdAsync(bucket, objectId, cancellationToken);
        if (obj == null)
            return null;

        var metadata = new ObjectMetadata
        {
            ObjectId = obj.ObjectId,
            Bucket = obj.Bucket,
            Size = obj.Length,
            ContentType = obj.ContentType,
            Hash = obj.Hash,
            Tier = obj.Tier,
            CreatedAt = obj.CreatedAt,
            LastAccessedAt = obj.LastAccessedAt
        };

        if (!string.IsNullOrEmpty(obj.Tags))
        {
            metadata.Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(obj.Tags);
        }

        return metadata;
    }

    public async Task DeleteAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default)
    {
        var obj = await _objectRepository.GetByBucketAndIdAsync(bucket, objectId, cancellationToken);
        if (obj == null)
            throw new ObjectNotFoundException(bucket, objectId);

        await _backend.DeleteAsync(obj.RelativePath, obj.Tier, cancellationToken);
        await _objectRepository.DeleteAsync(objectId, cancellationToken);

        _logger.LogInformation("Deleted object {ObjectId} from bucket {Bucket}", objectId, bucket);
    }

    public async Task<ListObjectsResult> ListObjectsAsync(ListObjectsRequest request, CancellationToken cancellationToken = default)
    {
        var skip = 0;
        if (!string.IsNullOrEmpty(request.ContinuationToken))
        {
            if (int.TryParse(request.ContinuationToken, out var tokenSkip))
                skip = tokenSkip;
        }

        var take = Math.Min(request.MaxKeys, 1000);
        var objects = await _objectRepository.ListByBucketAsync(request.Bucket, request.Prefix, skip, take + 1, cancellationToken);

        var hasMore = objects.Count > take;
        if (hasMore)
            objects = objects.Take(take).ToList();

        var result = new ListObjectsResult
        {
            Bucket = request.Bucket,
            Objects = objects.Select(o => new ObjectMetadata
            {
                ObjectId = o.ObjectId,
                Bucket = o.Bucket,
                Size = o.Length,
                ContentType = o.ContentType,
                Hash = o.Hash,
                Tier = o.Tier,
                CreatedAt = o.CreatedAt,
                LastAccessedAt = o.LastAccessedAt,
                Tags = !string.IsNullOrEmpty(o.Tags) ? JsonSerializer.Deserialize<Dictionary<string, string>>(o.Tags) : null
            }).ToList(),
            IsTruncated = hasMore,
            NextContinuationToken = hasMore ? (skip + take).ToString() : null
        };

        return result;
    }

    public async Task ChangeTierAsync(string bucket, Guid objectId, StorageTier newTier, CancellationToken cancellationToken = default)
    {
        var obj = await _objectRepository.GetByBucketAndIdAsync(bucket, objectId, cancellationToken);
        if (obj == null)
            throw new ObjectNotFoundException(bucket, objectId);

        if (obj.Tier == newTier)
        {
            _logger.LogInformation("Object {ObjectId} is already in tier {Tier}", objectId, newTier);
            return;
        }

        await _backend.MoveTierAsync(obj.RelativePath, obj.Tier, newTier, cancellationToken);
        await _objectRepository.UpdateTierAsync(objectId, newTier, cancellationToken);

        _logger.LogInformation("Changed tier for object {ObjectId} from {OldTier} to {NewTier}", objectId, obj.Tier, newTier);
    }

    private async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private void ValidateContentType(string contentType)
    {
        if (_options.AllowedContentTypes == null || !_options.AllowedContentTypes.Any())
            return;

        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new StorageException($"Content type {contentType} is not allowed");
        }
    }
}

public class StorageServiceOptions
{
    public ShardingConfig Shard { get; set; } = new();
    public int MaxFileSizeMb { get; set; }
    public List<string>? AllowedContentTypes { get; set; }
}
