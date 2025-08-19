using AutoMapper;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Domain.Interfaces;

namespace Credo.FileStorage.Application.Features.Todo.Queries.GetTodo;

public class GetTodoQueryHandler(
    ITodoQueryRepository todoQueryRepository,
    IMapper mapper
) : IQueryHandler<GetTodoQuery, GetTodoDto>
{
    public async Task<Result<GetTodoDto>> Handle(
        GetTodoQuery request,
        CancellationToken cancellationToken
    )
    {
        var repositoryResult = await todoQueryRepository.Get(request.Id, cancellationToken);

        if (!repositoryResult.IsSuccess)
        {
            return Result.Failure<GetTodoDto>(repositoryResult.Errors);
        }

        if (repositoryResult.Value is null)
        {
            return Result.Failure<GetTodoDto>(DomainErrors.Todo.NotFound(request.Id));
        }

        var mappedResult = mapper.Map<GetTodoDto>(repositoryResult.Value);

        return Result.Create(mappedResult);
    }
}