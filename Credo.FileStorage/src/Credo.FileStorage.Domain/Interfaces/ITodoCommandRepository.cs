using Credo.Core.Shared.Library;
using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Domain.Interfaces;

public interface ITodoCommandRepository
{
    public Task<Result<Guid>> Create(Todo todo, CancellationToken cancellationToken);
}