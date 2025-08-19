using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Application.Features.Todo.Commands.CreateTodo;
using Credo.FileStorage.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Todo.Commands.CreateTodo;

[Collection("AutoMapperCollectionFixture")]
public class CreateTodoCommandHandlerTests : IClassFixture<CreateTodoCommandHandlerFixture>
{
    private readonly ITodoCommandRepository _todoCommandRepository;
    private readonly CreateTodoCommandHandler _handler;
    private readonly CreateTodoCommandValidator _validator;

    public CreateTodoCommandHandlerTests(
        CreateTodoCommandHandlerFixture createTodoCommandHandlerFixture
    )
    {
        _todoCommandRepository = createTodoCommandHandlerFixture.TodoCommandRepository;
        _handler = createTodoCommandHandlerFixture.CreateTodoCommandHandler;
        _validator = createTodoCommandHandlerFixture.Validator;
    }

    [Fact]
    [Trait("Category", "Todo Command")]
    public async Task Create_ShouldReturnGuid_WhenTodoIsCreated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var command = new CreateTodoCommand("Test");

        _todoCommandRepository
            .Create(Arg.Any<Domain.Models.Todo>(), Arg.Any<CancellationToken>())
            .Returns(id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
    }

    [Fact]
    [Trait("Category", "Todo Command")]
    public async Task Create_ShouldReturnFailure_WhenTodoNameIsInvalid()
    {
        // Arrange
        var command = new CreateTodoCommand("This is a very long name");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(n => n.ErrorMessage == DomainErrors.Todo.NameIsTooLong.Message);
    }

    // test when name is valid
    [Fact]
    [Trait("Category", "Todo Command")]
    public async Task Create_ShouldReturnFailure_WhenTodoNameIsValid()
    {
        // Arrange
        var command = new CreateTodoCommand("Short");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}