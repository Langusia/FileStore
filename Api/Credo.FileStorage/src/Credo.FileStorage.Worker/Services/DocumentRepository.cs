using System.Data;
using System.Runtime.CompilerServices;
using Credo.FileStorage.Worker.Interfaces;
using Credo.FileStorage.Worker.Models;
using Microsoft.Data.SqlClient;

namespace Credo.FileStorage.Worker.Services;

public class DocumentRepository : IDocumentRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DocumentRepository> _logger;
    
    public DocumentRepository(string connectionString, ILogger<DocumentRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }
    
    public async IAsyncEnumerable<DocumentMetadata> GetPendingMetadataAsync(
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Optimized: Use existing FileSize column, EXISTS instead of JOIN for better performance
        var query = @"
            SELECT 
                d.DocumentID,
                d.ChannelId,
                d.DocumentName,
                d.DocumentExt,
                d.RecordDate,
                d.ContentType,
                d.FileSize
            FROM Documents.dbo.Documents d WITH (NOLOCK)
            WHERE d.DelStatus = 0 
            AND EXISTS (
                SELECT 1 FROM Documents.dbo.DocumentsContent dc WITH (NOLOCK)
                WHERE dc.Id = d.DocumentID
            )
            AND NOT EXISTS (
                SELECT 1 FROM Documents.dbo.MigrationStatus ms WITH (NOLOCK)
                WHERE ms.ContentId = d.DocumentID 
                AND ms.Status = 'Success'
            )
            ORDER BY d.DocumentID
            OPTION (MAXDOP 4)";
        
        await using var command = new SqlCommand(query, connection);
        command.CommandTimeout = 300;
        
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, // Stream data efficiently
            cancellationToken);
        
        var batch = new List<DocumentMetadata>(batchSize);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var metadata = new DocumentMetadata
            {
                DocumentID = reader.GetInt64(0),
                ChannelId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                DocumentName = reader.IsDBNull(2) ? null : reader.GetString(2),
                DocumentExt = reader.IsDBNull(3) ? null : reader.GetString(3),
                RecordDate = reader.GetDateTime(4),
                ContentType = reader.IsDBNull(5) ? null : reader.GetString(5),
                FileSize = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
            };
            
            batch.Add(metadata);
            
            if (batch.Count >= batchSize)
            {
                foreach (var item in batch)
                {
                    yield return item;
                }
                batch.Clear();
            }
        }
        
        foreach (var item in batch)
        {
            yield return item;
        }
    }
    
    public async Task<byte[]> GetDocumentContentAsync(
        long documentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Simple, direct query - no optimization needed for single row fetch
        var query = @"
            SELECT Documents 
            FROM Documents.dbo.DocumentsContent WITH (NOLOCK)
            WHERE Id = @DocumentID";
        
        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@DocumentID", SqlDbType.BigInt).Value = documentId;
        command.CommandTimeout = 120;
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        if (result == null || result == DBNull.Value)
        {
            _logger.LogWarning("Document content not found for DocumentID: {DocumentID}", documentId);
            return null;
        }
        
        return (byte[])result;
    }
    
    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Optimized: Use COUNT_BIG for large tables, NOLOCK for dirty read (acceptable for counts)
        var query = @"
            SELECT COUNT_BIG(*)
            FROM Documents.dbo.Documents d WITH (NOLOCK)
            WHERE d.DelStatus = 0 
            AND EXISTS (
                SELECT 1 FROM Documents.dbo.DocumentsContent dc WITH (NOLOCK)
                WHERE dc.Id = d.DocumentID
            )
            AND NOT EXISTS (
                SELECT 1 FROM Documents.dbo.MigrationStatus ms WITH (NOLOCK)
                WHERE ms.ContentId = d.DocumentID 
                AND ms.Status = 'Success'
            )";
        
        await using var command = new SqlCommand(query, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }
}