using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Repositories;

public interface IStorageOperationRepository
{
    Task<IEnumerable<StorageOperation>> GetAllAsync();
    Task<StorageOperation> GetByIdAsync(Guid id);
    Task<StorageOperation> CreateAsync(StorageOperation storageOperation);
    Task<StorageOperation> UpdateAsync(StorageOperation storageOperation);
    Task DeleteAsync(Guid id);
    Task<StorageOperation?> GetByAliasAsync(string alias);
} 