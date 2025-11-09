using FileStore.Storage.Enums;

namespace FileStore.Storage.Services;

/// <summary>
/// Provides naming strategies for buckets and object keys.
/// Ensures consistent naming conventions across the system.
/// </summary>
public interface INamingStrategy
{
    /// <summary>
    /// Generates a bucket name based on channel and operation.
    /// Format: {channel}-{operation}
    /// Example: "web-user-uploads", "mobile-documents"
    /// </summary>
    string GenerateBucketName(Channel channel, Operation operation);

    /// <summary>
    /// Generates a unique object key (filename) with timestamp and GUID to avoid collisions.
    /// Format: {sanitizedFileName}_{timestamp}_{guid}{extension}
    /// Example: "invoice_20250109150623_a1b2c3d4-e5f6.pdf"
    /// </summary>
    string GenerateObjectKey(string originalFileName);

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    string SanitizeFileName(string fileName);
}

/// <summary>
/// Default implementation of naming strategy.
/// </summary>
public class DefaultNamingStrategy : INamingStrategy
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars()
        .Concat(new[] { ' ', '(', ')', '[', ']', '{', '}' })
        .ToArray();

    public string GenerateBucketName(Channel channel, Operation operation)
    {
        var channelStr = channel.ToStringValue();
        var operationStr = operation.ToStringValue();

        // Format: channel-operation
        // Example: web-user-uploads, mobile-documents
        return $"{channelStr}-{operationStr}";
    }

    public string GenerateObjectKey(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name cannot be null or empty.", nameof(originalFileName));
        }

        // Extract extension
        var extension = Path.GetExtension(originalFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

        // Sanitize the filename
        var sanitizedName = SanitizeFileName(fileNameWithoutExtension);

        // Generate timestamp in format: yyyyMMddHHmmss
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        // Generate a short GUID (first 8 characters)
        var guid = Guid.NewGuid().ToString("N")[..8];

        // Format: {sanitizedFileName}_{timestamp}_{guid}{extension}
        return $"{sanitizedName}_{timestamp}_{guid}{extension}";
    }

    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "file";
        }

        // Replace invalid characters with underscore
        var sanitized = string.Join("", fileName.Select(c =>
            InvalidFileNameChars.Contains(c) ? '_' : c));

        // Remove multiple consecutive underscores
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

        // Trim underscores from start and end
        sanitized = sanitized.Trim('_');

        // Ensure not empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "file";
        }

        // Limit length to 200 characters
        if (sanitized.Length > 200)
        {
            sanitized = sanitized[..200];
        }

        return sanitized;
    }
}
