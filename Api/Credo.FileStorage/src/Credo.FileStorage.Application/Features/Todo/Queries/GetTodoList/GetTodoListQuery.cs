using Credo.Core.Shared.Mediator;

namespace Credo.FileStorage.Application.Features.Todo.Queries.GetTodoList;

public record GetTodoListQuery : IQuery<List<GetTodoListDto>>;