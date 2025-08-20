using System.Data.SqlClient;
using Credo.Core.Shared.Library;
using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using Credo.FileStorage.Domain.Settings;
using Dapper;
using Microsoft.Extensions.Options;

namespace Credo.FileStorage.Persistence;

public class TodoQueryRepository(
    IOptions<ConnectionStrings> connectionStrings
) : ITodoQueryRepository
{
    private readonly string _connectionString = connectionStrings.Value.Todo;

    public async Task<Result<Todo?>> Get(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            var result = await connection.QuerySingleOrDefaultAsync<Todo>(
                @"SELECT * FROM Todos WHERE Id = @id",
                new { id }
            );

            return result;
        }
        catch (Exception e)
        {
            return Result.Failure<Todo?>(DomainErrors.DbError.Error("Todos", e.Message));
        }
    }

    public async Task<Result<List<Todo>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<Todo>(
                @"SELECT * FROM Todos"
            );

            return results.ToList();
        }
        catch (Exception e)
        {
            return Result.Failure<List<Todo>>(DomainErrors.DbError.Error("Todos", e.Message));
        }
    }
}