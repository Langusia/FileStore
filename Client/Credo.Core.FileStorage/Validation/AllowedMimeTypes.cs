public static class DocumentTypeCodes
{
    public const short Csv = 120;
    public const short Pdf = 101;
    public const short Png = 201;
    public const short Jpeg = 202;
    public const short Zip = 301;

    public const short Xlsx = 311;
}

public static class MimeMap
{
    public static string ToContentType(short typeCode) => typeCode switch
    {
        DocumentTypeCodes.Csv => "text/csv",
        DocumentTypeCodes.Pdf => "application/pdf",
        DocumentTypeCodes.Png => "image/png",
        DocumentTypeCodes.Jpeg => "image/jpeg",
        DocumentTypeCodes.Zip => "application/zip",
        DocumentTypeCodes.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };

    public static string? PreferredExtensionForMime(string mime) => mime switch
    {
        "text/csv" => "csv",
        "application/pdf" => "pdf",
        "image/png" => "png",
        "image/jpeg" => "jpg",
        "application/zip" => "zip",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "xlsx",
        _ => null
    };
}