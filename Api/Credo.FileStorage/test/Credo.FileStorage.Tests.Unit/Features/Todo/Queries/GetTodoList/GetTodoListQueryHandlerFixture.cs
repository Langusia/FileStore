using Credo.FileStorage.Application.Features.Todo.Queries.GetTodoList;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Tests.Unit.Fixtures;
using NSubstitute;
using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Todo.Queries.GetTodoList;

[Collection("AutoMapperCollectionFixture")]
public class GetTodoListQueryHandlerFixture
{
    public readonly ITodoQueryRepository TodoQueryRepository = Substitute.For<ITodoQueryRepository>();
    public readonly GetTodoListQueryHandler GetTodoListQueryHandler;

    public GetTodoListQueryHandlerFixture(AutoMapperFixture autoMapperFixture)
    {
        GetTodoListQueryHandler = new GetTodoListQueryHandler(TodoQueryRepository, autoMapperFixture.Mapper);
    }
}