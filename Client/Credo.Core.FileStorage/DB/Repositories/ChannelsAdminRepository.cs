using Credo.Core.FileStorage.Entities;
using Dapper;

namespace Credo.Core.FileStorage.DB.Repositories;

public sealed class ChannelsAdminRepository(IDbConnectionFactory dbf) : IChannelsAdminRepository
{
    public async Task<IEnumerable<Channel>> GetAllAsync(CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Channels ORDER BY Alias";
        return await con.QueryAsync<Channel>(sql);
    }

    public async Task<Channel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Channels WHERE Id=@id";
        return await con.QuerySingleOrDefaultAsync<Channel>(sql, new { id });
    }

    public async Task<Channel> CreateAsync(Channel channel, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "INSERT INTO doc.Channels (Id, Alias, ExternalAlias, ExternalId) VALUES (@Id, @Alias, @ExternalAlias, @ExternalId)";
        if (channel.Id == Guid.Empty) channel.Id = Guid.NewGuid();
        await con.ExecuteAsync(sql, channel);
        return channel;
    }

    public async Task<Channel> UpdateAsync(Channel channel, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "UPDATE doc.Channels SET Alias=@Alias, ExternalAlias=@ExternalAlias, ExternalId=@ExternalId WHERE Id=@Id";
        await con.ExecuteAsync(sql, channel);
        return channel;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "DELETE FROM doc.Channels WHERE Id=@id";
        await con.ExecuteAsync(sql, new { id });
    }
}


