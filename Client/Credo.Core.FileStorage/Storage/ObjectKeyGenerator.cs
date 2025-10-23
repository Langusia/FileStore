namespace Credo.Core.FileStorage.Storage;

/// <summary>
/// Generates object keys with collision-resistant naming
/// </summary>
internal static class ObjectKeyGenerator
{
    /// <summary>
    /// Builds an object key with date prefix and GUID for collision resistance.
    /// Format: {prefix}/{yyyy-MM-dd}-{guid}.{ext} or {prefix}-{guid}.{ext}
    /// </summary>
    /// <param name="prefix">Optional prefix (defaults to current date)</param>
    /// <param name="extension">File extension without dot</param>
    /// <param name="timestamp">Timestamp for date-based prefix</param>
    /// <returns>Generated object key</returns>
    public static string Generate(string? prefix, string extension, DateTime timestamp)
    {
        var effectivePrefix = DeterminePrefix(prefix, timestamp);
        var uniqueId = Guid.NewGuid().ToString("N");

        return string.IsNullOrEmpty(extension)
            ? $"{effectivePrefix}-{uniqueId}"
            : $"{effectivePrefix}-{uniqueId}.{extension}";
    }

    /// <summary>
    /// Determines the effective prefix: uses provided prefix or generates date-based prefix
    /// </summary>
    private static string DeterminePrefix(string? prefix, DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return $"{timestamp:yyyy}-{timestamp:MM}-{timestamp:dd}";
        }

        return prefix.Trim().Trim('/');
    }

    /// <summary>
    /// Extracts file extension, preferring MIME-based extension over original filename
    /// </summary>
    public static string DetermineExtension(string mimeType, string originalFileName)
    {
        return MimeMap.PreferredExtensionForMime(mimeType)
               ?? Path.GetExtension(originalFileName).TrimStart('.').ToLowerInvariant();
    }
}