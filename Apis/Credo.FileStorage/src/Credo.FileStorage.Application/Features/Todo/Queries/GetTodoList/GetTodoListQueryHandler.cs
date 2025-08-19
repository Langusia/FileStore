using AutoMapper;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Domain.Interfaces;

namespace Credo.FileStorage.Application.Features.Todo.Queries.GetTodoList;

public class GetTodoListQueryHandler(
    ITodoQueryRepository todoQueryRepository,
    IMapper mapper
) : IQueryHandler<GetTodoListQuery, List<GetTodoListDto>>
{
    public async Task<Result<List<GetTodoListDto>>> Handle(
        GetTodoListQuery request,
        CancellationToken cancellationToken
    )
    {
        // get repository result
        var repositoryResult = await todoQueryRepository.GetAll(cancellationToken);

        if (!repositoryResult.IsSuccess)
        {
            return Result.Failure<List<GetTodoListDto>>(repositoryResult.Errors);
        }

        // check if list is empty
        if (repositoryResult.Value.Count is 0)
        {
            return Result.Failure<List<GetTodoListDto>>(DomainErrors.Todo.ListEmpty);
        }

        // map domain model to dto
        var dto = mapper.Map<List<GetTodoListDto>>(repositoryResult.Value);

        return Result.Create(dto);
    }
}