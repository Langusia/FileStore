using Credo.Core.FileStorage.Validation;

namespace Credo.FileStorage.Worker.LegacyFetcher;

public static class KeyBuilder
{
    public static string Build(long id, string ext) => $"docs/{id}.{ext}";
}

public static class TypeMap
{
    public static (string Mime, string Ext) From(int? documentTypeId, string? contentType, string? documentExt)
    {
        // First try to use the ContentType if available
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return (contentType, GetExtensionFromContentType(contentType));
        }

        // Then try to use DocumentTypeID
        if (documentTypeId.HasValue)
        {
            return documentTypeId.Value switch
            {
                1 => ("application/pdf", "pdf"),
                2 => ("image/png", "png"),
                3 => ("image/jpeg", "jpg"),
                4 => ("application/zip", "zip"),
                5 => ("text/csv", "csv"),
                6 => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
                _ => ("application/octet-stream", "bin")
            };
        }

        // Finally, try to use the DocumentExt
        if (!string.IsNullOrWhiteSpace(documentExt))
        {
            var ext = documentExt.TrimStart('.').ToLowerInvariant();
            return (GetContentTypeFromExtension(ext), ext);
        }

        // Default fallback
        return ("application/octet-stream", "bin");
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => "pdf",
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "application/zip" => "zip",
            "text/csv" => "csv",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "xlsx",
            _ => "bin"
        };
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "zip" => "application/zip",
            "csv" => "text/csv",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}