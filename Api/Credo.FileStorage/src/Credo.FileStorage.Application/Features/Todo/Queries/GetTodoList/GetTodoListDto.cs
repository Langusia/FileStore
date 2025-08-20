using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Application.Features.Todo.Queries.GetTodoList;

public record GetTodoListDto(string Name, TodoStatus Status);