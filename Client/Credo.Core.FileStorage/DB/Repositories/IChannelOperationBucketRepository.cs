using Credo.Core.FileStorage.Entities;

namespace Credo.Core.FileStorage.DB.Repositories;

public interface IChannelOperationBucketRepository
{
    // Key lookup (no side effects)
    Task<ChannelOperationBucket> GetByIdAsync(Guid channelOperationBucketId, CancellationToken ct = default);

    // Derived lookups (no side effects, throw if not found)
    Task<ChannelOperationBucket> GetByAliasesAsync(string channelAlias, string operationAlias, CancellationToken ct = default);
    Task<ChannelOperationBucket> GetByExternalAliasesAsync(string channelExternalAlias, string operationExternalAlias, CancellationToken ct = default);
    Task<ChannelOperationBucket> GetByExternalIdsAsync(long channelExternalId, long operationExternalId, CancellationToken ct = default);

    // Side-effecting “create if missing”
    Task<ChannelOperationBucket> GetOrCreateDefaultForBucketAsync(string bucketName, CancellationToken ct = default);

    // Optional null-returning variants (no exceptions)
    //Task<ChannelOperationBucket?> TryGetByAliasesAsync(string channelAlias, string operationAlias, CancellationToken ct = default);
    //Task<ChannelOperationBucket?> TryGetByExternalAliasesAsync(string channelExternalAlias, string operationExternalAlias, CancellationToken ct = default);
    //Task<ChannelOperationBucket?> TryGetByExternalIdsAsync(long channelExternalId, long operationExternalId, CancellationToken ct = default);
    //Task<ChannelOperationBucket?> TryGetByIdAsync(Guid channelOperationBucketId, CancellationToken ct = default);
}