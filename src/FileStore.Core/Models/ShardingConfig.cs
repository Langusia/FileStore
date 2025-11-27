namespace FileStore.Core.Models;

public class ShardingConfig
{
    public int Levels { get; set; } = 3;
    public int CharsPerShard { get; set; } = 2;
}
