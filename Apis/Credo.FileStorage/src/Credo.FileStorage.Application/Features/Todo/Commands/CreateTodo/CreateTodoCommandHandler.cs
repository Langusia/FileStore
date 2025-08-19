using AutoMapper;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using Credo.FileStorage.Domain.Interfaces;

namespace Credo.FileStorage.Application.Features.Todo.Commands.CreateTodo;

public class CreateTodoCommandHandler(
    ITodoCommandRepository todoCommandRepository,
    IMapper mapper
)
    : ICommandHandler<CreateTodoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = mapper.Map<Domain.Models.Todo>(request);

        var repositoryResult = await todoCommandRepository.Create(todo, cancellationToken);

        return !repositoryResult.IsSuccess
            ? Result.Failure<Guid>(repositoryResult.Errors)
            : Result.Success(repositoryResult.Value);
    }
}