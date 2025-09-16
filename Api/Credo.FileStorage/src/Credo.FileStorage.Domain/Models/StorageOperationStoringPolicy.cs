namespace Credo.FileStorage.Domain.Models;

public sealed class StorageOperationStoringPolicy
{
    public Guid Id { get; set; }
    public Guid StorageOperationId { get; set; }
    public string Name { get; set; } = null!;
    public int TransitionInDays { get; set; }
    public int? ExpirationInDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

