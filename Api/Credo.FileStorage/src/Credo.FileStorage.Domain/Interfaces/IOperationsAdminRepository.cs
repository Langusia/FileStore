using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface IOperationsAdminRepository
{
    Task<List<OperationAdmin>> GetAll(CancellationToken cancellationToken);
    Task<OperationAdmin?> Get(Guid id, CancellationToken cancellationToken);
    Task<OperationAdmin> Create(OperationAdmin operation, CancellationToken cancellationToken);
    Task<OperationAdmin> Update(OperationAdmin operation, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}


