namespace Credo.Core.FileStorage.V1.Entities;

public sealed class StoringPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int? TransitionInDays { get; set; }
    public int? ExpirationInDays { get; set; }

    public ICollection<BucketStoringPolicy> BucketStoringPolicies { get; set; } = new List<BucketStoringPolicy>();
}