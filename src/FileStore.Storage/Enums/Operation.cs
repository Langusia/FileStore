namespace FileStore.Storage.Enums;

/// <summary>
/// Represents the operation type for object storage.
/// Used for bucket naming and categorization.
/// </summary>
public enum Operation
{
    UserUploads = 1,
    Documents = 2,
    Images = 3,
    Videos = 4,
    Exports = 5,
    Reports = 6,
    Backups = 7,
    Temp = 8,
    Archives = 9
}

/// <summary>
/// Extension methods for Operation enum providing string conversion.
/// </summary>
public static class OperationExtensions
{
    private static readonly Dictionary<Operation, string> OperationToString = new()
    {
        { Operation.UserUploads, "user-uploads" },
        { Operation.Documents, "documents" },
        { Operation.Images, "images" },
        { Operation.Videos, "videos" },
        { Operation.Exports, "exports" },
        { Operation.Reports, "reports" },
        { Operation.Backups, "backups" },
        { Operation.Temp, "temp" },
        { Operation.Archives, "archives" }
    };

    private static readonly Dictionary<string, Operation> StringToOperation =
        OperationToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Converts the Operation enum to its string representation.
    /// </summary>
    public static string ToStringValue(this Operation operation)
    {
        return OperationToString.TryGetValue(operation, out var value)
            ? value
            : operation.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Converts the Operation enum to its integer representation.
    /// </summary>
    public static int ToIntValue(this Operation operation)
    {
        return (int)operation;
    }

    /// <summary>
    /// Parses a string value to Operation enum.
    /// </summary>
    public static Operation ParseOperation(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Operation value cannot be null or empty.", nameof(value));

        if (StringToOperation.TryGetValue(value, out var operation))
            return operation;

        throw new ArgumentException($"Invalid operation value: {value}", nameof(value));
    }

    /// <summary>
    /// Tries to parse a string value to Operation enum.
    /// </summary>
    public static bool TryParseOperation(string value, out Operation operation)
    {
        operation = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return StringToOperation.TryGetValue(value, out operation);
    }
}
