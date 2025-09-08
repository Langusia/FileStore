namespace Credo.Core.FileStorage.Validation;

public interface IFileTypeInspector
{
    Task<short> DetectOrThrowAsync(Stream content, string fileName, string? providedMime, CancellationToken ct);
}