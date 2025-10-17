using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeDetective;
using MimeDetective.Definitions;

namespace Credo.Core.FileStorage.Validation.MimeProbes;

public sealed class ExcelProbe : IFileTypeProbe
{
    private readonly IContentInspector _inspector = new ContentInspectorBuilder
    {
        Definitions = DefaultDefinitions.All()
    }.Build();

    public Task<short?> TryDetectAsync(Stream content, string fileName, string? providedMime,
        byte[] head, FileTypeInspectorOptions opts, CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        var result = _inspector.Inspect(head);
        var def = result.ByMimeType().FirstOrDefault();

        if (def != null)
        {
            var mt = def.MimeType?.ToLowerInvariant();
            if (mt == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                || mt == "application/vnd.ms-excel.sheet.macroenabled.12")
            {
                return Task.FromResult((short?)DocumentTypeCodes.Xlsx);
            }

            if (mt == "application/vnd.ms-excel")
            {
                return Task.FromResult((short?)DocumentTypeCodes.Xls);
            }

            // ZIP-based detection can indicate OpenXML; use extension to disambiguate
            if (mt == "application/zip")
            {
                if (ext == ".xlsx" || ext == ".xlsm" || ext == ".xltx" || ext == ".xltm")
                    return Task.FromResult((short?)DocumentTypeCodes.Xlsx);
            }
        }

        // Fallback to provided mime string
        if (!string.IsNullOrEmpty(providedMime))
        {
            var pm = providedMime.ToLowerInvariant();
            if (pm.Contains("spreadsheetml") || pm.Contains("vnd.ms-excel"))
                return Task.FromResult((short?)(ext == ".xls" ? DocumentTypeCodes.Xls : DocumentTypeCodes.Xlsx));
        }

        // Final fallback to extension only
        if (ext == ".xls") return Task.FromResult((short?)DocumentTypeCodes.Xls);
        if (ext == ".xlsx" || ext == ".xlsm" || ext == ".xltx" || ext == ".xltm")
            return Task.FromResult((short?)DocumentTypeCodes.Xlsx);

        return Task.FromResult<short?>(null);
    }
}