using Credo.Core.Shared.Mediator;

namespace Credo.FileStorage.Application.Features.Todo.Commands.CreateTodo;

public record CreateTodoCommand(string Name) : ICommand<Guid>;