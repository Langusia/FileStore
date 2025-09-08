namespace Credo.Core.FileStorage.Validation.MimeProbes;

public interface IFileTypeProbe
{
    Task<short?> TryDetectAsync(
        Stream content, string fileName, string? providedMime,
        byte[] head, FileTypeInspectorOptions opts, CancellationToken ct);
}