using FileStore.Core.Enums;

namespace FileStore.Core.Interfaces;

public interface IFileStorageBackend
{
    Task<string> StoreAsync(Stream stream, string relativePath, StorageTier tier, CancellationToken cancellationToken = default);
    Task<Stream> RetrieveAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default);
    Task<long> GetSizeAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default);
    Task MoveTierAsync(string relativePath, StorageTier fromTier, StorageTier toTier, CancellationToken cancellationToken = default);
}
