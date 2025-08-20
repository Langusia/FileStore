using Credo.Core.Shared.Library;
using MediatR;

namespace Credo.Core.Shared.Mediator;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}