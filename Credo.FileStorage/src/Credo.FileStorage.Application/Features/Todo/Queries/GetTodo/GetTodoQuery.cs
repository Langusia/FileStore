using Credo.Core.Shared.Mediator;

namespace Credo.FileStorage.Application.Features.Todo.Queries.GetTodo;

public record GetTodoQuery(Guid Id) : IQuery<GetTodoDto>;