using System.Data.SqlClient;
using Credo.Core.Shared.Library;
using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using Credo.FileStorage.Domain.Settings;
using Dapper;
using Microsoft.Extensions.Options;

namespace Credo.FileStorage.Persistence;

public class TodoCommandRepository(
    IOptions<ConnectionStrings> connectionStrings
) : ITodoCommandRepository
{
    private readonly string _connectionString = connectionStrings.Value.DocumentDb;

    public async Task<Result<Guid>> Create(Todo todo, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            await connection.ExecuteAsync(
                @$"INSERT INTO Todos (Id, Name, Status) VALUES (@Id, @Name, 0)",
                new { todo.Id, todo.Name }
            );

            return todo.Id;
        }
        catch (Exception e)
        {
            return Result.Failure<Guid>(DomainErrors.DbError.Error("Todos", e.Message));
        }
    }
}