namespace Credo.Core.Minio.Models;

public struct FileToStore
{
    public readonly required string BucketName { get; init; }
    public readonly string? Prefix { get; init; }
    public readonly Stream Stream { get; init; }
    public readonly required string Name { get; init; }
    public readonly required string ContentType { get; init; }
    public StoringPolicy? StoringPolicy { get; init; }
}