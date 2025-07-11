using Credo.Core.Minio.Models;
using System.Text.RegularExpressions;

namespace Credo.Core.Minio.Storage;

public interface IMinioStorage
{
    Task StoreFile(FileToStore fileToStore, CancellationToken cancellationToken, StoringPolicy? storingPolicy = null);
    Task<byte[]> GetFile(string bucketName, string objectName);
    Task<string[]> PutBucketAsync(string bucketName, CancellationToken token, StoringPolicy? storingPolicy = null);
    Task DeleteFile(string bucketName, string objectName, CancellationToken cancellationToken);
}