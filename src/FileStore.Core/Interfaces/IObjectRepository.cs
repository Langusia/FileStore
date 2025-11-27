using FileStore.Core.Enums;
using FileStore.Core.Models;

namespace FileStore.Core.Interfaces;

public interface IObjectRepository
{
    Task<StoredObject?> GetByIdAsync(Guid objectId, CancellationToken cancellationToken = default);
    Task<StoredObject?> GetByBucketAndIdAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default);
    Task CreateAsync(StoredObject obj, CancellationToken cancellationToken = default);
    Task UpdateLastAccessedAsync(Guid objectId, DateTime accessedAt, CancellationToken cancellationToken = default);
    Task UpdateTierAsync(Guid objectId, StorageTier tier, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid objectId, CancellationToken cancellationToken = default);
    Task<List<StoredObject>> ListByBucketAsync(string bucket, string? prefix, int skip, int take, CancellationToken cancellationToken = default);
    Task<List<StoredObject>> GetObjectsForTieringAsync(int coldAfterDays, List<string> excludedBuckets, int batchSize, CancellationToken cancellationToken = default);
}
