using Credo.FileStorage.Application.Features.Todo.Queries.GetTodo;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Tests.Unit.Fixtures;
using NSubstitute;
using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Todo.Queries.GetTodo;

[Collection("AutoMapperCollectionFixture")]
public class GetTodoQueryHandlerFixture
{
    public readonly ITodoQueryRepository TodoQueryRepository = Substitute.For<ITodoQueryRepository>();
    public readonly GetTodoQueryHandler GetTodoQueryHandler;

    public GetTodoQueryHandlerFixture(AutoMapperFixture autoMapperFixture)
    {
        GetTodoQueryHandler = new GetTodoQueryHandler(TodoQueryRepository, autoMapperFixture.Mapper);
    }
}