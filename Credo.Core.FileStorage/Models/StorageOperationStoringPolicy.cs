using Credo.Core.Minio.Models;

namespace Credo.Core.FileStorage.Models;

public record StorageOperationStoringPolicy
{
    public StorageOperationStoringPolicy(Guid Id, Guid StorageOperationId, string Name, int TransitionInDays, int? ExpirationInDays)
    {
        this.Id = Id;
        this.StorageOperationId = StorageOperationId;
        this.Name = Name;
        this.TransitionInDays = TransitionInDays;
        this.ExpirationInDays = ExpirationInDays;
    }

    public StorageOperationStoringPolicy()
    {
    }

    public Guid Id { get; init; }
    public Guid StorageOperationId { get; init; }
    public string Name { get; init; }
    public int TransitionInDays { get; init; }
    public int? ExpirationInDays { get; init; }

    public void Deconstruct(out Guid Id, out Guid StorageOperationId, out string Name, out int TransitionInDays, out int? ExpirationInDays)
    {
        Id = this.Id;
        StorageOperationId = this.StorageOperationId;
        Name = this.Name;
        TransitionInDays = this.TransitionInDays;
        ExpirationInDays = this.ExpirationInDays;
    }

    public StoringPolicy ToStoringPolicy()
    {
        return new StoringPolicy(TransitionInDays, ExpirationInDays);
    }
}
