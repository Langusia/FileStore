namespace Credo.Core.FileStorage.V1.DB.Models.Upload;

using System.Runtime.CompilerServices;
#if HAS_ASPNETCORE
using Microsoft.AspNetCore.Http;
#endif

/// <summary>
/// A unified container for file content + metadata passed to the storage SDK.
/// </summary>
public sealed record UploadFile(
    Stream Content,
    string FileName,
    string? ContentType = null,
    long? DeclaredLength = null,
    bool DisposeStream = false) : IAsyncDisposable, IDisposable
{
    // Factory helpers

    public static UploadFile FromStream(
        Stream stream, string fileName, string? contentType = null,
        long? declaredLength = null, bool disposeStream = false)
        => new(stream, fileName, contentType, declaredLength, disposeStream);

    public static UploadFile FromBytes(
        byte[] bytes, string fileName, string? contentType = null, bool disposeStream = true)
        => new(new MemoryStream(bytes, writable: false), fileName, contentType, bytes.LongLength, disposeStream);

    public static UploadFile FromFilePath(
        string path, string? contentType = null, bool disposeStream = true)
    {
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        return new UploadFile(fs, Path.GetFileName(path), contentType, fs.Length, disposeStream);
    }

#if HAS_ASPNETCORE
    public static UploadFile FromFormFile(
        IFormFile file, bool disposeStream = true)
        => new(file.OpenReadStream(), file.FileName, file.ContentType, file.Length, disposeStream);
#endif

    // Ownership helpers

    public void Dispose()
    {
        if (DisposeStream)
            Content.Dispose();
    }

    public ValueTask DisposeAsync()
        => DisposeStream ? Content.DisposeAsync() : ValueTask.CompletedTask;
}