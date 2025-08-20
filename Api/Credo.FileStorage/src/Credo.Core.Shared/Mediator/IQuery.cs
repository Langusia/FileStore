using Credo.Core.Shared.Library;
using MediatR;

namespace Credo.Core.Shared.Mediator;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}