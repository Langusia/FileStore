using System.Data.SqlClient;
using Credo.FileStorage.Application.Errors;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using Credo.FileStorage.Domain.Settings;
using Dapper;
using Microsoft.Extensions.Options;

namespace Credo.FileStorage.Persistence;

public sealed class ChannelsAdminRepository(IOptions<ConnectionStrings> cs) : IChannelsAdminRepository
{
    private readonly string _connectionString = cs.Value.DocumentDb;

    public async Task<List<ChannelAdmin>> GetAll(CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        var res = await con.QueryAsync<ChannelAdmin>("SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Channels ORDER BY Alias");
        return res.ToList();
    }

    public async Task<ChannelAdmin?> Get(Guid id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        return await con.QuerySingleOrDefaultAsync<ChannelAdmin>("SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Channels WHERE Id=@id", new { id });
    }

    public async Task<ChannelAdmin> Create(ChannelAdmin channel, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        if (channel.Id == Guid.Empty) channel.Id = Guid.NewGuid();
        await con.ExecuteAsync("INSERT INTO doc.Channels (Id, Alias, ExternalAlias, ExternalId) VALUES (@Id, @Alias, @ExternalAlias, @ExternalId)", channel);
        return channel;
    }

    public async Task<ChannelAdmin> Update(ChannelAdmin channel, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        await con.ExecuteAsync("UPDATE doc.Channels SET Alias=@Alias, ExternalAlias=@ExternalAlias, ExternalId=@ExternalId WHERE Id=@Id", channel);
        return channel;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        await con.ExecuteAsync("DELETE FROM doc.Channels WHERE Id=@id", new { id });
    }
}


