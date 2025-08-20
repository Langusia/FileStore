using System.Data;
using Credo.Core.FileStorage.Repositories;

namespace Credo.Core.FileStorage;

public class UnitOfWork : IDisposable
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public IChannelRepository ChannelRepository { get; }
    public IOperationRepository OperationRepository { get; }
    public IFileMetadataRepository FileMetadataRepository { get; }
    public IBucketMetadataRepository BucketMetadataRepository { get; }
    public IStorageOperationRepository StorageOperationRepository { get; }
    public IStorageOperationStoringPolicyRepository StorageOperationStoringPolicyRepository { get; }

    public UnitOfWork(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
        _transaction = _connection.BeginTransaction();

        ChannelRepository = new ChannelRepository(_connection, _transaction);
        OperationRepository = new OperationRepository(_connection, _transaction);
        FileMetadataRepository = new FileMetadataRepository(_connection, _transaction);
        BucketMetadataRepository = new BucketMetadataRepository(_connection, _transaction);
        StorageOperationRepository = new StorageOperationRepository(_connection, _transaction);
        StorageOperationStoringPolicyRepository = new StorageOperationStoringPolicyRepository(_connection, _transaction);
    }

    public void Commit() => _transaction.Commit();
    public void Rollback() => _transaction.Rollback();

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}