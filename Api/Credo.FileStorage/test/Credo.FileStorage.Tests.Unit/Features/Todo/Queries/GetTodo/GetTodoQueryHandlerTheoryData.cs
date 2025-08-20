using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Todo.Queries.GetTodo;

public class GetTodoQueryHandlerTheoryData : TheoryData<Guid>
{
    public GetTodoQueryHandlerTheoryData()
    {
        Add(Guid.Parse("282D5B25-E851-441D-B08C-5B7AC886880C"));
    }
}