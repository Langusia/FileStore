using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Repositories;

public interface IFileMetadataRepository
{
    Task<IEnumerable<FileMetadata>> GetAllAsync();
    Task<FileMetadata> GetByIdAsync(Guid id);
    Task<FileMetadata> CreateAsync(FileMetadata file);
    Task<FileMetadata> UpdateAsync(FileMetadata file);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<FileMetadata>> GetByBucketIdAsync(Guid bucketMetadataId);
} 