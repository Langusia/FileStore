using Credo.Core.Shared.Library;

namespace Credo.FileStorage.Application.Errors;

public static class DomainErrors
{
    public static class Todo
    {
        public static readonly Func<Guid, Error> NotFound = id => new(
            "Todo.NotFound",
            $"The todo with the identifier {id} was not found.",
            ErrorTypeEnum.NotFound
        );

        public static readonly Error NameIsTooLong = new(
            "Todo.NameIsTooLong",
            "Name of todo is too long",
            ErrorTypeEnum.BadRequest
        );

        public static readonly Error ListEmpty = new(
            "Todo.ListEmpty",
            "Todo list is empty",
            ErrorTypeEnum.NoContent
        );
    }

    public static class DbError
    {
        public static readonly Func<string, string, Error> Error = (db, message) => new(
            "DbError.Error",
            $"Database error DB: {db} with message: {message}",
            ErrorTypeEnum.InternalServerError
        );
    }
}