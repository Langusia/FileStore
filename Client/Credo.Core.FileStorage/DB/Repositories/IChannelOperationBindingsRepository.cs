using Credo.Core.FileStorage.DB.Entities;

namespace Credo.Core.FileStorage.DB.Repositories;

public interface IChannelOperationBindingsRepository
{
    Task<IEnumerable<ChannelOperationBucket>> GetAllAsync(CancellationToken ct = default);
    Task<ChannelOperationBucket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChannelOperationBucket> CreateAsync(Guid channelId, Guid operationId, Guid bucketId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}


