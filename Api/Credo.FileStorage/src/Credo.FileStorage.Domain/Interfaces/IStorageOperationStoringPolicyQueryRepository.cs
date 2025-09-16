using Credo.Core.Shared.Library;
using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface IStorageOperationStoringPolicyQueryRepository
{
    Task<Result<StorageOperationStoringPolicy?>> Get(Guid id, CancellationToken cancellationToken);
    Task<Result<List<StorageOperationStoringPolicy>>> GetAll(CancellationToken cancellationToken);
    Task<Result<StorageOperationStoringPolicy?>> GetByStorageOperationId(Guid storageOperationId, CancellationToken cancellationToken);
}

