using Credo.Core.FileStorage.Models.Upload;

namespace Credo.Core.FileStorage.Storage;

/// <summary>
/// Utility class for stream operations
/// </summary>
internal static class StreamHelper
{
    private const int MaxMemoryStreamSize = 100 * 1024 * 1024; // 100 MB
    private const int BufferSize = 64 * 1024; // 64 KB

    /// <summary>
    /// Creates a pre-sized MemoryStream for reading, or default capacity for large files
    /// </summary>
    public static MemoryStream CreateMemoryStream(long size)
    {
        return (size > 0 && size <= MaxMemoryStreamSize)
            ? new MemoryStream(capacity: (int)size)
            : new MemoryStream();
    }

    /// <summary>
    /// Ensures a stream is seekable for content type detection.
    /// Non-seekable streams are copied to a temporary file.
    /// </summary>
    public static async Task<(Stream stream, long size, bool isTempFile)> EnsureSeekableAsync(
        Stream input,
        CancellationToken ct = default)
    {
        if (input.CanSeek)
        {
            if (input.Position != 0) input.Position = 0;
            return (input, input.Length, false);
        }

        // Create temp file for non-seekable streams
        var tempPath = Path.Combine(Path.GetTempPath(), $"upload_{Guid.NewGuid():N}.tmp");
        var fs = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: BufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

        await input.CopyToAsync(fs, BufferSize, ct);
        fs.Position = 0;
        return (fs, fs.Length, true);
    }

    /// <summary>
    /// Cleans up temporary and disposable streams
    /// </summary>
    public static async Task CleanupStreamsAsync(Stream stream, bool isTempStream, UploadFile? originalFile = null)
    {
        // Dispose temp stream
        if (isTempStream)
        {
            try { await stream.DisposeAsync(); }
            catch { /* Ignore disposal errors for temp streams */ }
        }

        // Dispose original file stream if requested
        if (originalFile?.DisposeStream == true)
        {
            try { await originalFile.DisposeAsync(); }
            catch { /* Ignore disposal errors */ }
        }
    }
}