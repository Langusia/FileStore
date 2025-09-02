namespace Credo.FileStorage.Domain.Models;

public sealed class ChannelOperationBinding
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid OperationId { get; set; }
    public Guid BucketId { get; set; }
}



