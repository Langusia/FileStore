using FileStore.Core.Enums;
using FileStore.Core.Exceptions;
using FileStore.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileStore.Infrastructure.Backends;

public class SmbStorageBackend : IFileStorageBackend
{
    private readonly SmbStorageOptions _options;
    private readonly ILogger<SmbStorageBackend> _logger;

    public SmbStorageBackend(IOptions<SmbStorageOptions> options, ILogger<SmbStorageBackend> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> StoreAsync(Stream stream, string relativePath, StorageTier tier, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath, tier);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await stream.CopyToAsync(fileStream, cancellationToken);
            _logger.LogInformation("Stored object at {Path} in {Tier} tier", fullPath, tier);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store object at {Path}", fullPath);
            throw new StorageException($"Failed to store object at {fullPath}", ex);
        }
    }

    public async Task<Stream> RetrieveAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath, tier);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Object not found at {Path}", fullPath);
            throw new StorageException($"Object not found at {fullPath}");
        }

        try
        {
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            _logger.LogInformation("Retrieved object from {Path} in {Tier} tier", fullPath, tier);
            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve object from {Path}", fullPath);
            throw new StorageException($"Failed to retrieve object from {fullPath}", ex);
        }
    }

    public Task<bool> ExistsAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath, tier);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath, tier);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Attempted to delete non-existent object at {Path}", fullPath);
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted object at {Path} from {Tier} tier", fullPath, tier);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object at {Path}", fullPath);
            throw new StorageException($"Failed to delete object at {fullPath}", ex);
        }
    }

    public Task<long> GetSizeAsync(string relativePath, StorageTier tier, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath, tier);

        if (!File.Exists(fullPath))
        {
            throw new StorageException($"Object not found at {fullPath}");
        }

        var fileInfo = new FileInfo(fullPath);
        return Task.FromResult(fileInfo.Length);
    }

    public async Task MoveTierAsync(string relativePath, StorageTier fromTier, StorageTier toTier, CancellationToken cancellationToken = default)
    {
        var sourcePath = GetFullPath(relativePath, fromTier);
        var destPath = GetFullPath(relativePath, toTier);

        if (!File.Exists(sourcePath))
        {
            throw new StorageException($"Source object not found at {sourcePath}");
        }

        var destDirectory = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
        {
            Directory.CreateDirectory(destDirectory);
        }

        try
        {
            File.Move(sourcePath, destPath, overwrite: false);
            _logger.LogInformation("Moved object from {FromTier} to {ToTier}: {Path}", fromTier, toTier, relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move object from {FromTier} to {ToTier}: {Path}", fromTier, toTier, relativePath);
            throw new StorageException($"Failed to move object from {fromTier} to {toTier}", ex);
        }
    }

    private string GetFullPath(string relativePath, StorageTier tier)
    {
        var rootPath = tier == StorageTier.Hot ? _options.HotRootPath : _options.ColdRootPath;
        return Path.Combine(rootPath, relativePath);
    }
}

public class SmbStorageOptions
{
    public string HotRootPath { get; set; } = string.Empty;
    public string ColdRootPath { get; set; } = string.Empty;
}
