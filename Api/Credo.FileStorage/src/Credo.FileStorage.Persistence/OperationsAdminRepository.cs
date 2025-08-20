using System.Data.SqlClient;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using Credo.FileStorage.Domain.Settings;
using Dapper;
using Microsoft.Extensions.Options;

namespace Credo.FileStorage.Persistence;

public sealed class OperationsAdminRepository(IOptions<ConnectionStrings> cs) : IOperationsAdminRepository
{
    private readonly string _connectionString = cs.Value.DocumentDb;

    public async Task<List<OperationAdmin>> GetAll(CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        var res = await con.QueryAsync<OperationAdmin>("SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Operations ORDER BY Alias");
        return res.ToList();
    }

    public async Task<OperationAdmin?> Get(Guid id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        return await con.QuerySingleOrDefaultAsync<OperationAdmin>("SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Operations WHERE Id=@id", new { id });
    }

    public async Task<OperationAdmin> Create(OperationAdmin operation, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        if (operation.Id == Guid.Empty) operation.Id = Guid.NewGuid();
        await con.ExecuteAsync("INSERT INTO doc.Operations (Id, Alias, ExternalAlias, ExternalId) VALUES (@Id, @Alias, @ExternalAlias, @ExternalId)", operation);
        return operation;
    }

    public async Task<OperationAdmin> Update(OperationAdmin operation, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        await con.ExecuteAsync("UPDATE doc.Operations SET Alias=@Alias, ExternalAlias=@ExternalAlias, ExternalId=@ExternalId WHERE Id=@Id", operation);
        return operation;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        await con.ExecuteAsync("DELETE FROM doc.Operations WHERE Id=@id", new { id });
    }
}


