namespace Credo.Core.FileStorage.Models.Download;

public sealed class StorageObject(string contentType, string fileName, Stream stream)
{
    public Stream Stream { get; init; } = stream;
    public string ContentType { get; init; } = contentType;
    public string FileName { get; init; } = fileName;
}