using Credo.Core.Shared.Library;
using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface IStorageOperationStoringPolicyCommandRepository
{
    Task<Result<Guid>> Create(StorageOperationStoringPolicy policy, CancellationToken cancellationToken);
    Task<Result> Update(StorageOperationStoringPolicy policy, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
}

