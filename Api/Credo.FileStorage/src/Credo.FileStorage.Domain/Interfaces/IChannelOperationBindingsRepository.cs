using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface IChannelOperationBindingsRepository
{
    Task<List<ChannelOperationBinding>> GetAll(CancellationToken cancellationToken);
    Task<ChannelOperationBinding?> Get(Guid id, CancellationToken cancellationToken);
    Task<ChannelOperationBinding> Create(ChannelOperationBinding binding, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}



