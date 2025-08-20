using Credo.Core.Minio.Models;

namespace Credo.Core.Minio.Storage;

public interface IMinioStorage
{
    Task StoreFile(FileToStore fileToStore, CancellationToken cancellationToken);
    Task<byte[]> GetFile(string bucketName, string objectName);
    Task PutBucketAsync(string bucketName, CancellationToken token, StoringPolicy? storingPolicy = null);
    Task DeleteFile(string bucketName, string objectName, CancellationToken cancellationToken);
}