namespace Credo.Core.FileStorage.V1.Entities;

public sealed class Operation
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = null!;
    public string? ExternalAlias { get; set; }
    public long? ExternalId { get; set; }

    public ICollection<ChannelOperationBucket> ChannelOperationBuckets { get; set; } = new List<ChannelOperationBucket>();
}
