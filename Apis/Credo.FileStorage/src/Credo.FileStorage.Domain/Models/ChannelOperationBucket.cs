namespace Credo.FileStorage.Domain.Models;

public sealed class ChannelOperationBucket
{
    public Guid Id { get; set; }

    public Guid OperationId { get; set; }
    public Guid ChannelId  { get; set; }
    public Guid BucketId   { get; set; }

    public Operation Operation { get; set; } = null!;
    public Channel  Channel  { get; set; } = null!;
    public Bucket   Bucket   { get; set; } = null!;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}