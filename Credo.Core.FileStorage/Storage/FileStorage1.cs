using System.Text.RegularExpressions;
using Credo.Core.FileStorage.Models;
using Credo.Core.Minio.Storage;
using Microsoft.Extensions.Logging;

namespace Credo.Core.FileStorage.Storage;

public class FileStorage1 : IFileStorage
{
    private readonly IMinioStorage _storage;
    private readonly Func<UnitOfWork> _unitOfWorkFactory;
    private readonly ILogger<FileStorage1> _logger;

    public FileStorage1(
        IMinioStorage storage,
        Func<UnitOfWork> unitOfWorkFactory,
        ILogger<FileStorage1> logger)
    {
        _storage = storage;
        _unitOfWorkFactory = unitOfWorkFactory;
        _logger = logger;
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
        var storageOperation = await uow.StorageOperationRepository.GetByIdAsync(operation.StorageOperationId);
        if (storageOperation == null)
            throw new InvalidOperationException($"StorageOperation for OperationId '{operation.Id}' does not exist.");

        // Get default storing policy from database if none provided
        var defaultPolicy = file.StoringPolicy;
        if (defaultPolicy == null)
        {
            var dbPolicy = await uow.StorageOperationStoringPolicyRepository.GetByStorageOperationIdAsync(storageOperation.Id);
            if (dbPolicy != null)
            {
                defaultPolicy = dbPolicy.ToStoringPolicy();
            }
        }

        // Construct bucket name using channel alias and storageOperation alias
        var fileToStore = file.ToFileToStore(channel, storageOperation, defaultPolicy);
        try
        {
            // 1. Store file in Minio
            await _storage.StoreFile(fileToStore, cancellationToken);
            var bucketMetadata = await uow.BucketMetadataRepository.GetByAliasAsync(fileToStore.BucketName);
            if (bucketMetadata is null)
            {
                bucketMetadata = await uow.BucketMetadataRepository.CreateAsync(new BucketMetadata
                {
                    Id = Guid.NewGuid(),
                    ChannelId = channel.Id,
                    Name = fileToStore.BucketName,
                    StorageOperationId = storageOperation.Id
                });
            }

            // 2. Store file metadata in DB
            var fileMetadata = new FileMetadata(
                bucketMetadata.Id,
                GenerateStoredFileName(fileToStore.Name),
                fileToStore.Stream.Length,
                fileToStore.ContentType,
                DateTime.UtcNow
            );
            await uow.FileMetadataRepository.CreateAsync(fileMetadata);

            uow.Commit();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, null);
            await _storage.DeleteFile(fileToStore.BucketName, fileToStore.Name, cancellationToken);
            uow.Rollback();
            throw;
        }
    }

    private static string GenerateStoredFileName(string originalFileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
        var guid = Guid.NewGuid().ToString();

        var extension = Path.GetExtension(originalFileName); // includes the dot (e.g. ".pdf")
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

        // Sanitize to only allow safe characters (alphanumeric, underscores, hyphens)
        var sanitized = Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9_\-]", "_");

        // Truncate if needed to avoid exceeding 1024 bytes (we’ll keep total name under 255 characters to be safe)
        var maxSanitizedLength = 255 - (timestamp.Length + guid.Length + extension.Length + 2); // +2 for underscores
        if (sanitized.Length > maxSanitizedLength)
        {
            sanitized = sanitized.Substring(0, maxSanitizedLength);
        }

        return $"{timestamp}_{guid}_{sanitized}{extension}";
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