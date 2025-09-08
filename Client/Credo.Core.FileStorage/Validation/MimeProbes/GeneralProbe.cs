using MimeDetective;
using MimeDetective.Definitions;

namespace Credo.Core.FileStorage.Validation.MimeProbes;

public sealed class MagicSignatureProbe : IFileTypeProbe
{
    private readonly IContentInspector _inspector = new ContentInspectorBuilder
    {
        Definitions = DefaultDefinitions.All()
    }.Build();

    public Task<short?> TryDetectAsync(Stream content, string fileName, string? providedMime,
        byte[] head, FileTypeInspectorOptions opts, CancellationToken ct)
    {
        var r = _inspector.Inspect(head);
        var def = r.ByMimeType().FirstOrDefault();
        if (def is null) return Task.FromResult<short?>(null);

        return Task.FromResult(def.MimeType.ToLowerInvariant() switch
        {
            "application/pdf" => (short?)DocumentTypeCodes.Pdf,
            "image/png" => DocumentTypeCodes.Png,
            "image/jpeg" => DocumentTypeCodes.Jpeg,
            "application/zip" => DocumentTypeCodes.Zip,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => DocumentTypeCodes.Xlsx,
            _ => null
        });
    }
}