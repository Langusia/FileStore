using Credo.FileStorage.Application.Features.Todo.Commands.CreateTodo;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Tests.Unit.Fixtures;
using NSubstitute;
using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Todo.Commands.CreateTodo;

[Collection("AutoMapperCollectionFixture")]
public class CreateTodoCommandHandlerFixture
{
    public readonly ITodoCommandRepository TodoCommandRepository = Substitute.For<ITodoCommandRepository>();
    public readonly CreateTodoCommandHandler CreateTodoCommandHandler;
    public readonly CreateTodoCommandValidator Validator;

    public CreateTodoCommandHandlerFixture(AutoMapperFixture autoMapperFixture)
    {
        CreateTodoCommandHandler = new CreateTodoCommandHandler(TodoCommandRepository, autoMapperFixture.Mapper);

        Validator = new CreateTodoCommandValidator();
    }
}