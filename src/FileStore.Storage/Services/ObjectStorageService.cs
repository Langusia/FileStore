using FileStore.Storage.Brokers;
using FileStore.Storage.Data;
using FileStore.Storage.Enums;
using FileStore.Storage.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FileStore.Storage.Services;

/// <summary>
/// Main service for object storage operations.
/// Handles transactional operations between S3 and metadata database.
/// </summary>
public interface IObjectStorageService
{
    Task<UploadResponse> UploadObjectAsync(UploadRequest request, CancellationToken cancellationToken = default);
    Task<GetObjectResponse> GetObjectAsync(string objectId, CancellationToken cancellationToken = default);
    Task<DeleteResponse> DeleteObjectAsync(string objectId, CancellationToken cancellationToken = default);
    Task<ObjectMetadataResponse?> GetObjectMetadataAsync(string objectId, CancellationToken cancellationToken = default);
    Task<List<ObjectMetadataResponse>> ListObjectsAsync(Channel? channel = null, Operation? operation = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}

public class ObjectStorageService : IObjectStorageService
{
    private readonly IObjectStorageBroker _storageBroker;
    private readonly FileStoreDbContext _dbContext;
    private readonly INamingStrategy _namingStrategy;
    private readonly ILogger<ObjectStorageService> _logger;

    public ObjectStorageService(
        IObjectStorageBroker storageBroker,
        FileStoreDbContext dbContext,
        INamingStrategy namingStrategy,
        ILogger<ObjectStorageService> logger)
    {
        _storageBroker = storageBroker ?? throw new ArgumentNullException(nameof(storageBroker));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _namingStrategy = namingStrategy ?? throw new ArgumentNullException(nameof(namingStrategy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UploadResponse> UploadObjectAsync(
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.Content == null)
            throw new ArgumentException("Content stream cannot be null.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(request));

        // Generate bucket name and object key
        var bucketName = _namingStrategy.GenerateBucketName(request.Channel, request.Operation);
        var objectKey = _namingStrategy.GenerateObjectKey(request.FileName);

        _logger.LogInformation(
            "Starting upload: FileName={FileName}, Bucket={BucketName}, ObjectKey={ObjectKey}",
            request.FileName,
            bucketName,
            objectKey);

        // Start a database transaction for metadata operations
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Ensure bucket exists in storage
            await _storageBroker.EnsureBucketExistsAsync(bucketName, cancellationToken);

            // 2. Get or create bucket metadata
            var bucket = await GetOrCreateBucketAsync(
                bucketName,
                request.Channel,
                request.Operation,
                cancellationToken);

            // 3. Upload to S3
            var (eTag, size) = await _storageBroker.UploadObjectAsync(
                bucketName,
                objectKey,
                request.Content,
                request.ContentType,
                request.Metadata,
                cancellationToken);

            // 4. Get the full storage URL
            var fullUrl = _storageBroker.GetObjectUrl(bucketName, objectKey);

            // 5. Create metadata record
            var storageObject = new StorageObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                BucketId = bucket.Id,
                ObjectKey = objectKey,
                OriginalFileName = request.FileName,
                FullStorageUrl = fullUrl,
                ContentType = request.ContentType,
                SizeInBytes = request.TrackSize ? size : null,
                ETag = eTag,
                CreatedAt = DateTime.UtcNow,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
            };

            _dbContext.Objects.Add(storageObject);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 6. Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded object: ObjectId={ObjectId}, ObjectKey={ObjectKey}",
                storageObject.ObjectId,
                objectKey);

            return new UploadResponse
            {
                ObjectId = storageObject.ObjectId,
                ObjectKey = objectKey,
                BucketName = bucketName,
                FullStorageUrl = fullUrl,
                SizeInBytes = storageObject.SizeInBytes,
                ETag = eTag,
                UploadedAt = storageObject.CreatedAt
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to upload object: FileName={FileName}, Bucket={BucketName}",
                request.FileName,
                bucketName);

            // Attempt cleanup: delete from S3 if it was uploaded
            try
            {
                await _storageBroker.DeleteObjectAsync(bucketName, objectKey, cancellationToken);
                _logger.LogInformation("Cleaned up S3 object after failed upload");
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup S3 object after error");
            }

            throw;
        }
    }

    public async Task<GetObjectResponse> GetObjectAsync(
        string objectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            throw new ArgumentException("Object ID cannot be null or empty.", nameof(objectId));

        _logger.LogInformation("Retrieving object: ObjectId={ObjectId}", objectId);

        // Get metadata from database
        var storageObject = await _dbContext.Objects
            .Include(o => o.Bucket)
            .FirstOrDefaultAsync(o => o.ObjectId == objectId && !o.IsDeleted, cancellationToken);

        if (storageObject == null)
        {
            throw new FileNotFoundException($"Object with ID '{objectId}' not found.");
        }

        // Get object from S3
        var (content, contentType, size, metadata) = await _storageBroker.GetObjectAsync(
            storageObject.Bucket.BucketName,
            storageObject.ObjectKey,
            cancellationToken);

        // Update last accessed timestamp
        storageObject.LastAccessedAt = DateTime.UtcNow;
        storageObject.Bucket.LastAccessedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully retrieved object: ObjectId={ObjectId}", objectId);

        return new GetObjectResponse
        {
            ObjectId = storageObject.ObjectId,
            ObjectKey = storageObject.ObjectKey,
            BucketName = storageObject.Bucket.BucketName,
            Content = content,
            ContentType = contentType ?? storageObject.ContentType,
            SizeInBytes = size ?? storageObject.SizeInBytes,
            ETag = storageObject.ETag,
            LastModified = storageObject.LastModifiedAt,
            Metadata = metadata
        };
    }

    public async Task<DeleteResponse> DeleteObjectAsync(
        string objectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            throw new ArgumentException("Object ID cannot be null or empty.", nameof(objectId));

        _logger.LogInformation("Deleting object: ObjectId={ObjectId}", objectId);

        var storageObject = await _dbContext.Objects
            .Include(o => o.Bucket)
            .FirstOrDefaultAsync(o => o.ObjectId == objectId && !o.IsDeleted, cancellationToken);

        if (storageObject == null)
        {
            throw new FileNotFoundException($"Object with ID '{objectId}' not found.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Delete from S3
            await _storageBroker.DeleteObjectAsync(
                storageObject.Bucket.BucketName,
                storageObject.ObjectKey,
                cancellationToken);

            // Soft delete in database
            storageObject.IsDeleted = true;
            storageObject.DeletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted object: ObjectId={ObjectId}", objectId);

            return new DeleteResponse
            {
                ObjectId = storageObject.ObjectId,
                ObjectKey = storageObject.ObjectKey,
                BucketName = storageObject.Bucket.BucketName,
                Success = true,
                DeletedAt = storageObject.DeletedAt.Value,
                Message = "Object deleted successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to delete object: ObjectId={ObjectId}", objectId);

            return new DeleteResponse
            {
                ObjectId = storageObject.ObjectId,
                ObjectKey = storageObject.ObjectKey,
                BucketName = storageObject.Bucket.BucketName,
                Success = false,
                DeletedAt = DateTime.UtcNow,
                Message = $"Failed to delete object: {ex.Message}"
            };
        }
    }

    public async Task<ObjectMetadataResponse?> GetObjectMetadataAsync(
        string objectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            throw new ArgumentException("Object ID cannot be null or empty.", nameof(objectId));

        var storageObject = await _dbContext.Objects
            .Include(o => o.Bucket)
            .FirstOrDefaultAsync(o => o.ObjectId == objectId && !o.IsDeleted, cancellationToken);

        if (storageObject == null)
            return null;

        var metadata = !string.IsNullOrEmpty(storageObject.Metadata)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(storageObject.Metadata)
            : null;

        return new ObjectMetadataResponse
        {
            ObjectId = storageObject.ObjectId,
            ObjectKey = storageObject.ObjectKey,
            BucketName = storageObject.Bucket.BucketName,
            OriginalFileName = storageObject.OriginalFileName,
            FullStorageUrl = storageObject.FullStorageUrl,
            ContentType = storageObject.ContentType,
            SizeInBytes = storageObject.SizeInBytes,
            CreatedAt = storageObject.CreatedAt,
            LastModifiedAt = storageObject.LastModifiedAt,
            LastAccessedAt = storageObject.LastAccessedAt,
            Metadata = metadata
        };
    }

    public async Task<List<ObjectMetadataResponse>> ListObjectsAsync(
        Channel? channel = null,
        Operation? operation = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Objects
            .Include(o => o.Bucket)
            .Where(o => !o.IsDeleted);

        if (channel.HasValue)
        {
            query = query.Where(o => o.Bucket.ChannelId == (int)channel.Value);
        }

        if (operation.HasValue)
        {
            query = query.Where(o => o.Bucket.OperationId == (int)operation.Value);
        }

        var objects = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return objects.Select(o =>
        {
            var metadata = !string.IsNullOrEmpty(o.Metadata)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(o.Metadata)
                : null;

            return new ObjectMetadataResponse
            {
                ObjectId = o.ObjectId,
                ObjectKey = o.ObjectKey,
                BucketName = o.Bucket.BucketName,
                OriginalFileName = o.OriginalFileName,
                FullStorageUrl = o.FullStorageUrl,
                ContentType = o.ContentType,
                SizeInBytes = o.SizeInBytes,
                CreatedAt = o.CreatedAt,
                LastModifiedAt = o.LastModifiedAt,
                LastAccessedAt = o.LastAccessedAt,
                Metadata = metadata
            };
        }).ToList();
    }

    private async Task<StorageBucket> GetOrCreateBucketAsync(
        string bucketName,
        Channel channel,
        Operation operation,
        CancellationToken cancellationToken)
    {
        var bucket = await _dbContext.Buckets
            .FirstOrDefaultAsync(b => b.BucketName == bucketName, cancellationToken);

        if (bucket == null)
        {
            bucket = new StorageBucket
            {
                BucketName = bucketName,
                ChannelId = (int)channel,
                ChannelName = channel.ToStringValue(),
                OperationId = (int)operation,
                OperationName = operation.ToStringValue(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Buckets.Add(bucket);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return bucket;
    }
}
