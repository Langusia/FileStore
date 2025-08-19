namespace Credo.Core.FileStorage.Models;

public record BucketMetadata
{
    public BucketMetadata(Guid Id,
        string Name,
        Guid ChannelId,
        Guid StorageOperationId)
    {
        this.Id = Id;
        this.Name = Name;
        this.ChannelId = ChannelId;
        this.StorageOperationId = StorageOperationId;
    }

    public BucketMetadata()
    {
    }

    public Guid Id { get; init; }
    public string Name { get; init; }
    public Guid ChannelId { get; init; }
    public Guid StorageOperationId { get; init; }

    public void Deconstruct(out Guid Id, out string Name, out Guid ChannelId, out Guid StorageOperationId)
    {
        Id = this.Id;
        Name = this.Name;
        ChannelId = this.ChannelId;
        StorageOperationId = this.StorageOperationId;
    }
}