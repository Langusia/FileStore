using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Repositories;

public interface IBucketMetadataRepository
{
    Task<IEnumerable<BucketMetadata>> GetAllAsync();
    Task<BucketMetadata> GetByIdAsync(Guid id);
    Task<BucketMetadata> CreateAsync(BucketMetadata bucket);
    Task<BucketMetadata> UpdateAsync(BucketMetadata bucket);
    Task DeleteAsync(Guid id);
} 