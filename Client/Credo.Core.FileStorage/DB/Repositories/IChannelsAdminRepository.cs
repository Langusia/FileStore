using Credo.Core.FileStorage.Entities;

namespace Credo.Core.FileStorage.DB.Repositories;

public interface IChannelsAdminRepository
{
    Task<IEnumerable<Channel>> GetAllAsync(CancellationToken ct = default);
    Task<Channel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Channel> CreateAsync(Channel channel, CancellationToken ct = default);
    Task<Channel> UpdateAsync(Channel channel, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}


