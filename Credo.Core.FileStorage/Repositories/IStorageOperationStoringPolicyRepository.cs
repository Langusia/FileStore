using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Repositories;

public interface IStorageOperationStoringPolicyRepository
{
    Task<IEnumerable<StorageOperationStoringPolicy>> GetAllAsync();
    Task<StorageOperationStoringPolicy> GetByIdAsync(Guid id);
    Task<StorageOperationStoringPolicy> CreateAsync(StorageOperationStoringPolicy storageOperationStoringPolicy);
    Task<StorageOperationStoringPolicy> UpdateAsync(StorageOperationStoringPolicy storageOperationStoringPolicy);
    Task DeleteAsync(Guid id);
    Task<StorageOperationStoringPolicy?> GetByStorageOperationIdAsync(Guid storageOperationId);
}
