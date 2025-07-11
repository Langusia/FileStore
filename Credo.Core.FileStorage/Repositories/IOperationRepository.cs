using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Repositories;

public interface IOperationRepository
{
    Task<IEnumerable<Operation>> GetAllAsync();
    Task<Operation> GetByIdAsync(Guid id);
    Task<Operation> CreateAsync(Operation operation);
    Task<Operation> UpdateAsync(Operation operation);
    Task DeleteAsync(Guid id);
    Task<Operation?> GetByAliasAsync(string alias);
} 