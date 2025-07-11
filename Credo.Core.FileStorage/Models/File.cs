using Credo.Core.Minio.Models;

namespace Credo.Core.FileStorage.Models;

public record CredoFile(Stream stream, string name, string contentType)
{
    internal FileToStore ToFileToStore(Channel channel, StorageOperation storageOperation) => new()
    {
        BucketName = $"{channel.Alias}/{storageOperation.Alias}",
        Stream = stream,
        Name = name,
        ContentType = contentType,
        Prefix = null
    };
}