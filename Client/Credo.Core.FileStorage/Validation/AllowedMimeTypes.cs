namespace Credo.Core.FileStorage.Validation;

public static class AllowedFileTypes
{
    // Canonical MIME values we accept (extend as needed)
    public static readonly HashSet<string> AllowedMimes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "text/csv",
        "application/vnd.ms-excel",                                         // .xls (legacy, OLE2)
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",// .xlsx
        "image/jpeg",
        "image/png",
        // optional:
        // "image/tiff",
        // "application/json",
        // "application/x-ofx", "application/vnd.intu.qfx"
    };

    // Helpful for fallback when sniff is inconclusive
    public static readonly Dictionary<string,string> ExtToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"]  = "application/pdf",
        [".csv"]  = "text/csv",
        [".xls"]  = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".jpeg"] = "image/jpeg",
        [".jpg"]  = "image/jpeg",
        [".png"]  = "image/png",
        // [".tif"] = "image/tiff", [".tiff"] = "image/tiff",
        // [".ofx"] = "application/x-ofx", [".qfx"] = "application/vnd.intu.qfx",
        // [".json"]= "application/json",
        // [".txt"] = "text/plain",
    };
}