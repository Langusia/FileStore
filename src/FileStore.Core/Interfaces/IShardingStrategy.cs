using FileStore.Core.Models;

namespace FileStore.Core.Interfaces;

public interface IShardingStrategy
{
    string ComputeRelativePath(Guid objectId, string extension, ShardingConfig config);
}
