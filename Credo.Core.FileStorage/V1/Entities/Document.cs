namespace Credo.Core.FileStorage.V1.Entities;

public sealed class Document
{
    public Guid Id { get; set; }

    public Guid ChannelOperationBucketId { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public long Size { get; set; }
    public short Type { get; set; } // map to enum if you like
    public DateTime UploadedAt { get; set; } // stored as DATETIME2(7) UTC

    public ChannelOperationBucket ChannelOperationBucket { get; set; } = null!;
}