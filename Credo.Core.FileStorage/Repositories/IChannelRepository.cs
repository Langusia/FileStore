using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Repositories;

public interface IChannelRepository
{
    Task<IEnumerable<Channel>> GetAllAsync();
    Task<Channel> GetByIdAsync(Guid id);
    Task<Channel> CreateAsync(Channel channel);
    Task<Channel> UpdateAsync(Channel channel);
    Task DeleteAsync(Guid id);
    Task<Channel?> GetByAliasAsync(string alias);
} 