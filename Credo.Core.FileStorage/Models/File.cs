using Credo.Core.Minio.Models;

namespace Credo.Core.FileStorage.Models;

public record CredoFile
{
    public CredoFile()
    {
    }

    public CredoFile(Stream Stream, string Name, string ContentType, StoringPolicy? storingPolicy = null)
    {
        this.Stream = Stream;
        this.Name = Name;
        this.ContentType = ContentType;
        StoringPolicy = storingPolicy;
    }

    public Stream Stream { get; init; }
    public string Name { get; init; }
    public string ContentType { get; init; }
    public StoringPolicy? StoringPolicy { get; init; }

    internal FileToStore ToFileToStore(Channel channel, StorageOperation storageOperation, StoringPolicy? defaultPolicy = null)
    {
        var s = new FileToStore
        {
            BucketName = $"{channel.Alias}--{storageOperation.Alias}",
            Stream = Stream,
            Name = Name,
            ContentType = ContentType,
            Prefix = null,
            StoringPolicy = this.StoringPolicy ?? defaultPolicy
        };
        return s;
    }
}