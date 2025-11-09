namespace FileStore.Storage.Enums;

/// <summary>
/// Represents the channel type for object storage operations.
/// Used for bucket naming and organizational purposes.
/// </summary>
public enum Channel
{
    Web = 1,
    Mobile = 2,
    Desktop = 3,
    API = 4,
    Integration = 5,
    Batch = 6,
    Admin = 7
}

/// <summary>
/// Extension methods for Channel enum providing string conversion.
/// </summary>
public static class ChannelExtensions
{
    private static readonly Dictionary<Channel, string> ChannelToString = new()
    {
        { Channel.Web, "web" },
        { Channel.Mobile, "mobile" },
        { Channel.Desktop, "desktop" },
        { Channel.API, "api" },
        { Channel.Integration, "integration" },
        { Channel.Batch, "batch" },
        { Channel.Admin, "admin" }
    };

    private static readonly Dictionary<string, Channel> StringToChannel =
        ChannelToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Converts the Channel enum to its string representation.
    /// </summary>
    public static string ToStringValue(this Channel channel)
    {
        return ChannelToString.TryGetValue(channel, out var value)
            ? value
            : channel.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Converts the Channel enum to its integer representation.
    /// </summary>
    public static int ToIntValue(this Channel channel)
    {
        return (int)channel;
    }

    /// <summary>
    /// Parses a string value to Channel enum.
    /// </summary>
    public static Channel ParseChannel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Channel value cannot be null or empty.", nameof(value));

        if (StringToChannel.TryGetValue(value, out var channel))
            return channel;

        throw new ArgumentException($"Invalid channel value: {value}", nameof(value));
    }

    /// <summary>
    /// Tries to parse a string value to Channel enum.
    /// </summary>
    public static bool TryParseChannel(string value, out Channel channel)
    {
        channel = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return StringToChannel.TryGetValue(value, out channel);
    }
}
