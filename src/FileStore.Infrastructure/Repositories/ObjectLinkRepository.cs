using System.Data;
using Dapper;
using FileStore.Core.Interfaces;
using FileStore.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FileStore.Infrastructure.Repositories;

public class ObjectLinkRepository : IObjectLinkRepository
{
    private readonly string _connectionString;

    public ObjectLinkRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task CreateAsync(ObjectLink link, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO ObjectLinks (ObjectId, Channel, Operation, BusinessEntityId, CreatedAt)
            VALUES (@ObjectId, @Channel, @Operation, @BusinessEntityId, @CreatedAt)";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, link, cancellationToken: cancellationToken));
    }

    public async Task<List<ObjectLink>> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT ObjectId, Channel, Operation, BusinessEntityId, CreatedAt
            FROM ObjectLinks
            WHERE ObjectId = @ObjectId";

        var results = await connection.QueryAsync<ObjectLink>(
            new CommandDefinition(sql, new { ObjectId = objectId }, cancellationToken: cancellationToken));

        return results.ToList();
    }

    public async Task DeleteByObjectIdAsync(Guid objectId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = "DELETE FROM ObjectLinks WHERE ObjectId = @ObjectId";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ObjectId = objectId }, cancellationToken: cancellationToken));
    }
}
