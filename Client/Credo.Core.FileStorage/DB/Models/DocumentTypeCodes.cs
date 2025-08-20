namespace Credo.Core.FileStorage.DB.Models;

public static class DocumentTypeCodes
{
    // keep in sync with your DB enum mapping (SMALLINT)
    // 1=pdf, 2=png, 3=jpeg, 4=docx, 5=txt, 6=csv, 7=json, 0=unknown
    private static readonly Dictionary<string, short> MapByExt = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = 1,
        [".png"] = 2,
        [".jpg"] = 3, [".jpeg"] = 3,
        [".docx"] = 4,
        [".txt"] = 5,
        [".csv"] = 6,
        [".json"] = 7
    };

    private static readonly Dictionary<string, short> MapByMime = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = 1,
        ["image/png"] = 2,
        ["image/jpeg"] = 3,
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = 4,
        ["text/plain"] = 5,
        ["text/csv"] = 6,
        ["application/json"] = 7
    };

    public static short From(string? contentType, string? extNoDot)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && MapByMime.TryGetValue(contentType!, out var t))
            return t;

        if (!string.IsNullOrWhiteSpace(extNoDot))
        {
            var dotExt = "." + extNoDot.Trim();
            if (MapByExt.TryGetValue(dotExt, out var e))
                return e;
        }

        return 0; // unknown
    }

    public static string ToContentType(short type) => type switch
    {
        1 => "application/pdf",
        2 => "image/png",
        3 => "image/jpeg",
        4 => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        5 => "text/plain",
        6 => "text/csv",
        7 => "application/json",
        _ => "application/octet-stream"
    };
}