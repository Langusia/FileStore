using FileStore.Core.Interfaces;
using FileStore.Core.Models;

namespace FileStore.Infrastructure.Services;

public class ShardingStrategy : IShardingStrategy
{
    public string ComputeRelativePath(Guid objectId, string extension, ShardingConfig config)
    {
        var guidString = objectId.ToString("N");

        var shardParts = new List<string>();
        var currentPos = 0;

        for (int i = 0; i < config.Levels; i++)
        {
            if (currentPos + config.CharsPerShard > guidString.Length)
                break;

            var shardPart = guidString.Substring(currentPos, config.CharsPerShard);
            shardParts.Add(shardPart);
            currentPos += config.CharsPerShard;
        }

        var fileName = $"{objectId:N}{extension}";
        var pathParts = shardParts.Concat(new[] { fileName });

        return Path.Combine(pathParts.ToArray());
    }
}
