using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Application.Features.Todo.Queries.GetTodo;

public record GetTodoDto(string Name, TodoStatus Status);