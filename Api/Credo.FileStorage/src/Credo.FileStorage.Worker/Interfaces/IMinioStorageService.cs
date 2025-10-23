namespace Credo.FileStorage.Worker.Interfaces;

public interface IMinioStorageService
{
    Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);
    
    Task UploadAsync(
        string bucketName,
        string objectKey,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);
    
    Task<bool> ObjectExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);
}