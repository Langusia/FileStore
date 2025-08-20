using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface IChannelOperationBucketsRepository
{
    Task<List<ChannelOperationBucket>> GetAll(CancellationToken cancellationToken);
    Task<ChannelOperationBucket?> Get(Guid id, CancellationToken cancellationToken);
    Task<ChannelOperationBucket> Create(ChannelOperationBucket binding, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}


