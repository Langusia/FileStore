using Credo.Core.FileStorage.Models.Upload;

namespace Credo.Core.FileStorage.Storage;

public interface IObjectStorage
{
    /// <summary>
    /// Uploads a file stream and records a document row atomically
    /// (MinIO upload + SQL insert with compensation on failure).
    /// The input stream is NOT disposed by this method.
    /// </summary>
    Task<UploadResult> Upload(
        IUploadRouteArgs route,
        Stream content,
        string fileName,
        string? contentType = null,
        UploadOptions? options = null,
        CancellationToken ct = default);

    Task<UploadResult> Upload(
        IUploadRouteArgs route,
        UploadFile file,
        UploadOptions? options = null,
        CancellationToken ct = default);
}