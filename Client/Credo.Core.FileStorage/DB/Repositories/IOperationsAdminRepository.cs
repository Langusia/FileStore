using Credo.Core.FileStorage.DB.Entities;

namespace Credo.Core.FileStorage.DB.Repositories;

public interface IOperationsAdminRepository
{
    Task<IEnumerable<Operation>> GetAllAsync(CancellationToken ct = default);
    Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Operation> CreateAsync(Operation operation, CancellationToken ct = default);
    Task<Operation> UpdateAsync(Operation operation, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}


