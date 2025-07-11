namespace Credo.Core.Minio.Models;

public struct FileToStore
{
    public readonly required string BucketName { get; init; }
    public readonly string? Prefix { get; init; }
    public readonly Stream Stream { get; init; }

    public readonly required string Name
    {
        get => Prefix is null ? Name : $"{Prefix}/{Name}";
        init => Name = value;
    }

    public readonly required string ContentType { get; init; }
}