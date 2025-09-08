using Credo.Core.FileStorage.Models.Download;
using Credo.Core.FileStorage.Models.Upload;

namespace Credo.Core.FileStorage.Storage;

public interface IObjectStorage
{
    Task<StorageObject> OpenReadAsync(Guid documentId, CancellationToken ct = default);
    Task<StorageObject> OpenReadAsync(string bucket, string objectKey, CancellationToken ct = default);

    Task<UploadResult> Upload(
        IUploadRouteArgs route,
        UploadFile file,
        UploadOptions? options = null,
        CancellationToken ct = default);
}