using System.Data;
using Dapper;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Repositories;

public class FileMetadataRepository : IFileMetadataRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public FileMetadataRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<FileMetadata> CreateAsync(FileMetadata file)
    {
        var sql = @"INSERT INTO doc.FileMetadatas (Id, BucketMetadataId, FileName, Size, ContentType, UploadedAt)
                    VALUES (@Id, @BucketMetadataId, @FileName, @Size, @ContentType, @UploadedAt)";
        await _connection.ExecuteAsync(sql, file, _transaction);
        return file;
    }

    public async Task<IEnumerable<FileMetadata>> GetAllAsync()
    {
        var sql = "SELECT * FROM doc.FileMetadatas";
        return await _connection.QueryAsync<FileMetadata>(sql, transaction: _transaction);
    }

    public async Task<FileMetadata> GetByIdAsync(Guid id)
    {
        var sql = "SELECT * FROM doc.FileMetadatas WHERE Id = @Id";
        return await _connection.QuerySingleOrDefaultAsync<FileMetadata>(sql, new { Id = id }, _transaction);
    }

    public async Task<FileMetadata> UpdateAsync(FileMetadata file)
    {
        var sql = @"UPDATE doc.FileMetadatas SET
                        BucketMetadataId = @BucketMetadataId,
                        FileName = @FileName,
                        Size = @Size,
                        ContentType = @ContentType,
                        UploadedAt = @UploadedAt
                    WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, file, _transaction);
        return file;
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM doc.FileMetadatas WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
    }

    public async Task<IEnumerable<FileMetadata>> GetByBucketIdAsync(Guid bucketMetadataId)
    {
        var sql = "SELECT * FROM doc.FileMetadatas WHERE BucketMetadataId = @BucketMetadataId";
        return await _connection.QueryAsync<FileMetadata>(sql, new { BucketMetadataId = bucketMetadataId }, _transaction);
    }
} 