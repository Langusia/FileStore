using Credo.Core.Shared.Library;
using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface ITodoQueryRepository
{
    public Task<Result<Todo?>> Get(Guid id, CancellationToken cancellationToken);

    public Task<Result<List<Todo>>> GetAll(CancellationToken cancellationToken);
}