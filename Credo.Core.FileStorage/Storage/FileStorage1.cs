using Credo.Core.FileStorage.Models;
using Credo.Core.Minio.Storage;

namespace Credo.Core.FileStorage.Storage;

public class FileStorage1 : IFileStorage
{
    private readonly IMinioStorage _storage;
    private readonly Func<UnitOfWork> _unitOfWorkFactory;

    public FileStorage1(
        IMinioStorage storage,
        Func<UnitOfWork> unitOfWorkFactory
    )
    {
        _storage = storage;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task Store(CredoFile file, Client client, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkFactory();
        // Validate Channel
        var channel = await uow.ChannelRepository.GetByAliasAsync(client.Channel);
        if (channel == null)
            throw new InvalidOperationException($"Channel with alias '{client.Channel}' does not exist.");
        // Validate Operation
        var operation = await uow.OperationRepository.GetByAliasAsync(client.Operation);
        if (operation == null)
            throw new InvalidOperationException($"Operation with alias '{client.Operation}' does not exist.");
        // Fetch StorageOperation by OperationId
        var storageOperation = (await uow.StorageOperationRepository.GetAllAsync()).FirstOrDefault(so => so.OperationId == operation.Id);
        if (storageOperation == null)
            throw new InvalidOperationException($"StorageOperation for OperationId '{operation.Id}' does not exist.");

        // Construct bucket name using channel alias and storageOperation alias
        var fileToStore = file.ToFileToStore(channel, storageOperation);
        try
        {
            // 1. Store file in Minio
            await _storage.StoreFile(fileToStore, cancellationToken);

            // 2. Store file metadata in DB
            var fileMetadata = new FileMetadata(
                Guid.NewGuid(),
                Guid.Empty, // TODO: set correct BucketMetadataId
                fileToStore.Name,
                fileToStore.Stream.Length,
                fileToStore.ContentType,
                DateTime.UtcNow
            );
            await uow.FileMetadataRepository.CreateAsync(fileMetadata);

            uow.Commit();
        }
        catch
        {
            await _storage.DeleteFile(fileToStore.BucketName, fileToStore.Name, cancellationToken);
            uow.Rollback();
            throw;
        }
    }

    public async Task<byte[]> Get(Client client, string objectName, CancellationToken cancellationToken)
    {
        var bucketName = client.ToBucketName();
        return await _storage.GetFile(bucketName, objectName);
    }

    public async Task CreateBucketAsync(BucketMetadata bucket, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkFactory();
        try
        {
            await _storage.PutBucketAsync(bucket.Name, cancellationToken);
            await uow.BucketMetadataRepository.CreateAsync(bucket);
            uow.Commit();
        }
        catch
        {
            uow.Rollback();
            throw;
        }
    }

    public async Task Delete(Client client, string objectName, CancellationToken cancellationToken)
    {
        var bucketName = client.ToBucketName();
        using var uow = _unitOfWorkFactory();
        try
        {
            // 1. Delete file from Minio
            await _storage.DeleteFile(bucketName, objectName, cancellationToken);

            // 2. Delete file metadata from DB (if you have a method for this, e.g., by file name and bucket)
            // This assumes you have a method to get the file metadata and delete by Id
            var allFiles = await uow.FileMetadataRepository.GetAllAsync();
            var fileMeta = allFiles.FirstOrDefault(f => f.FileName == objectName);
            if (fileMeta != null)
            {
                await uow.FileMetadataRepository.DeleteAsync(fileMeta.Id);
            }

            uow.Commit();
        }
        catch
        {
            uow.Rollback();
            throw;
        }
    }
}