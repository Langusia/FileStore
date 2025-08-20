namespace Credo.FileStorage.Domain.Models;

public sealed class ChannelAdmin
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = null!;
    public string? ExternalAlias { get; set; }
    public long? ExternalId { get; set; }
}


