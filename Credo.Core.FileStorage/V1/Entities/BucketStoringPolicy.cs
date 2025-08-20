namespace Credo.Core.FileStorage.V1.Entities;

public sealed class BucketStoringPolicy
{
    public Guid Id { get; set; }

    public Guid BucketId { get; set; }
    public Guid StoringPolicyId { get; set; }

    public Bucket Bucket { get; set; } = null!;
    public StoringPolicy StoringPolicy { get; set; } = null!;
}