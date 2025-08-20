using System.Data;
using Credo.Core.FileStorage.Models;
using Dapper;

namespace Credo.Core.FileStorage.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public ChannelRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<Channel> CreateAsync(Channel channel)
    {
        var sql = @"INSERT INTO doc.Channels (Id, Alias, Name, SourceId)
                    VALUES (@Id, @Alias, @Name, @SourceId)";
        await _connection.ExecuteAsync(sql, channel, _transaction);
        return channel;
    }

    public async Task<IEnumerable<Channel>> GetAllAsync()
    {
        var sql = "SELECT * FROM doc.Channels";
        return await _connection.QueryAsync<Channel>(sql, transaction: _transaction);
    }

    public async Task<Channel> GetByIdAsync(Guid id)
    {
        var sql = "SELECT * FROM doc.Channels WHERE Id = @Id";
        return await _connection.QuerySingleOrDefaultAsync<Channel>(sql, new { Id = id }, _transaction);
    }

    public async Task<Channel> UpdateAsync(Channel channel)
    {
        var sql = @"UPDATE doc.Channels SET
                        Alias = @Alias,
                        Name = @Name,
                        SourceId = @SourceId
                    WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, channel, _transaction);
        return channel;
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM doc.Channels WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
    }

    public async Task<Channel?> GetByAliasAsync(string alias)
    {
        var sql = "SELECT * FROM doc.Channels WHERE Alias = @Alias";
        return await _connection.QuerySingleOrDefaultAsync<Channel>(sql, new { Alias = alias }, _transaction);
    }
}