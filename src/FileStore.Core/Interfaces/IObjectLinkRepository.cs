using FileStore.Core.Models;

namespace FileStore.Core.Interfaces;

public interface IObjectLinkRepository
{
    Task CreateAsync(ObjectLink link, CancellationToken cancellationToken = default);
    Task<List<ObjectLink>> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken = default);
    Task DeleteByObjectIdAsync(Guid objectId, CancellationToken cancellationToken = default);
}
