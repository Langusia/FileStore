using Credo.Core.Shared.Library;
using MediatR;

namespace Credo.Core.Shared.Mediator;

public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}