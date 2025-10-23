using Credo.Core.FileStorage.Models.Download;
using Credo.Core.FileStorage.Models.Upload;

namespace Credo.Core.FileStorage.Storage;

public interface IObjectStorage
{
    Task<StorageObject> OpenReadAsync(Guid documentId, CancellationToken ct = default);
    Task<StorageObject> OpenReadAsync(string bucket, string objectKey, CancellationToken ct = default);

    Task<UploadResult> UploadAsync(
        IUploadRouteArgs route,
        UploadFile file,
        UploadOptions? options = null,
        CancellationToken ct = default);

    Task PutObjectAsync(
        string bucketName, 
        string objectKey, 
        Stream data, 
        long size, 
        string contentType, 
        CancellationToken ct = default);

    Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct = default);
    
    Task<bool> ObjectExistsAsync(string bucketName, string objectKey, CancellationToken ct = default);
    
    Task<ObjectMetadata?> GetObjectMetadataAsync(string bucketName, string objectKey, CancellationToken ct = default);
}

public record ObjectMetadata(
    string Bucket,
    string ObjectKey,
    long Size,
    string? ContentType,
    string? ETag,
    DateTime LastModified
);