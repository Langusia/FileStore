using Credo.Core.Shared.Library;
using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Application.Features.Todo.Queries.GetTodo;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Todo.Queries.GetTodo;

[Collection("AutoMapperCollectionFixture")]
public class GetTodoQueryHandlerTests : IClassFixture<GetTodoQueryHandlerFixture>
{
    private readonly ITodoQueryRepository _todoQueryRepository;
    private readonly GetTodoQueryHandler _handler;

    public GetTodoQueryHandlerTests(
        GetTodoQueryHandlerFixture getTodoListQueryHandlerFixture
    )
    {
        _todoQueryRepository = getTodoListQueryHandlerFixture.TodoQueryRepository;
        _handler = getTodoListQueryHandlerFixture.GetTodoQueryHandler;
    }

    [Theory]
    [ClassData(typeof(GetTodoQueryHandlerTheoryData))]
    [Trait("Category", "Todo Query")]
    public async Task Get_ShouldReturnGetTodoDto_WhenTodoExists(Guid id)
    {
        // Arrange
        var todo = new Domain.Models.Todo
        {
            Id = id,
            Name = "Test",
            Status = TodoStatus.New
        };
        var query = new GetTodoQuery(id);

        _todoQueryRepository.Get(id, Arg.Any<CancellationToken>()).Returns(todo);
        var expectedResult = new GetTodoDto(todo.Name, todo.Status);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    [Trait("Category", "Todo Query")]
    public async Task Get_ShouldReturnNotFound_WhenTodoDoNotExist()
    {
        // Arrange
        var todo = new Domain.Models.Todo
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Status = TodoStatus.New
        };
        var query = new GetTodoQuery(todo.Id);

        _todoQueryRepository
            .Get(Arg.Any<Guid>(), CancellationToken.None)
            .Returns(Result.Success(null as Domain.Models.Todo));
        var expectedResult = Result.Failure<GetTodoDto>(DomainErrors.Todo.NotFound(todo.Id));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(expectedResult.Errors);
    }
}