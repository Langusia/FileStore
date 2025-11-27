using FileStore.Core.Enums;
using FileStore.Core.Models;

namespace FileStore.Core.Interfaces;

public interface IStorageService
{
    Task<UploadResult> UploadAsync(UploadRequest request, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default);
    Task<ObjectMetadata?> GetMetadataAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default);
    Task<ListObjectsResult> ListObjectsAsync(ListObjectsRequest request, CancellationToken cancellationToken = default);
    Task ChangeTierAsync(string bucket, Guid objectId, StorageTier newTier, CancellationToken cancellationToken = default);
}
