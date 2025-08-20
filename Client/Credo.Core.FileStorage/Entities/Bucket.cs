namespace Credo.Core.FileStorage.Entities;

public sealed class Bucket
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<ChannelOperationBucket> ChannelOperationBuckets { get; set; } = new List<ChannelOperationBucket>();
    public ICollection<BucketStoringPolicy> BucketStoringPolicies { get; set; } = new List<BucketStoringPolicy>();
}