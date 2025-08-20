using Credo.Core.FileStorage.V1.DB.Models.Upload;

namespace Credo.Core.FileStorage.V1.Storage;

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