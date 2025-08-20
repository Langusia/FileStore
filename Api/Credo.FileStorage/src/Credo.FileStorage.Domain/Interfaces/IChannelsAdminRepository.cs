using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface IChannelsAdminRepository
{
    Task<List<ChannelAdmin>> GetAll(CancellationToken cancellationToken);
    Task<ChannelAdmin?> Get(Guid id, CancellationToken cancellationToken);
    Task<ChannelAdmin> Create(ChannelAdmin channel, CancellationToken cancellationToken);
    Task<ChannelAdmin> Update(ChannelAdmin channel, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}


