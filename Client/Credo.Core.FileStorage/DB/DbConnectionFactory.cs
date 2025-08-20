using Microsoft.Data.SqlClient;

namespace Credo.Core.FileStorage.DB;

public interface IDbConnectionFactory
{
    Task<SqlConnection> OpenAsync(CancellationToken ct = default);
}

public sealed class SqlConnectionFactory(string cs) : IDbConnectionFactory
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var con = new SqlConnection(cs);
        await con.OpenAsync(ct);
        return con; // repo disposes via await using
    }
}