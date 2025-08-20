using Credo.Core.Shared.Library;
using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Application.Features.Todo.Queries.GetTodo;
using Credo.FileStorage.Application.Features.Todo.Queries.GetTodoList;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Todo.Queries.GetTodoList;

[Collection("AutoMapperCollectionFixture")]
public class GetTodoListQueryHandlerTests : IClassFixture<GetTodoListQueryHandlerFixture>
{
    private readonly ITodoQueryRepository _todoQueryRepository;
    private readonly GetTodoListQueryHandler _handler;

    public GetTodoListQueryHandlerTests(
        GetTodoListQueryHandlerFixture getTodoListQueryHandlerFixture
    )
    {
        _todoQueryRepository = getTodoListQueryHandlerFixture.TodoQueryRepository;
        _handler = getTodoListQueryHandlerFixture.GetTodoListQueryHandler;
    }

    [Fact]
    [Trait("Category", "Todo Query")]
    public async Task GetTodoList_ShouldReturnListOfGetTodoDto_WhenTodosExists()
    {
        // Arrange
        var todos = new List<Domain.Models.Todo>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Status = TodoStatus.New
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test2",
                Status = TodoStatus.New
            }
        };
        var query = new GetTodoListQuery();

        _todoQueryRepository.GetAll(Arg.Any<CancellationToken>()).Returns(todos);
        var expectedResult = Result.Create(new List<GetTodoDto>
        {
            new(todos[0].Name, todos[0].Status),
            new(todos[1].Name, todos[1].Status)
        });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResult.Value);
    }

    [Fact]
    [Trait("Category", "Todo Query")]
    public async Task GetTodoList_ShouldReturnEmptyListResult_WhenTodosDoNotExist()
    {
        // Arrange
        var todos = new List<Domain.Models.Todo>();
        var query = new GetTodoListQuery();

        _todoQueryRepository.GetAll(CancellationToken.None).Returns(todos);
        var expectedResult = Result.Failure<List<GetTodoDto>>(DomainErrors.Todo.ListEmpty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(expectedResult.Errors);
    }
}