using System.ComponentModel.DataAnnotations;

namespace Credo.SomeClient.API.Dtos;

public record StorageOperationStoringPolicyDto
{
    public Guid Id { get; init; }
    public Guid StorageOperationId { get; init; }
    public string Name { get; init; }
    public int TransitionInDays { get; init; }
    public int? ExpirationInDays { get; init; }
}

public record CreateStorageOperationStoringPolicyRequest
{
    [Required]
    public Guid StorageOperationId { get; init; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "TransitionInDays must be greater than 0")]
    public int TransitionInDays { get; init; }
    
    [Range(1, int.MaxValue, ErrorMessage = "ExpirationInDays must be greater than 0")]
    public int? ExpirationInDays { get; init; }
}

public record UpdateStorageOperationStoringPolicyRequest
{
    [Required]
    public Guid Id { get; init; }
    
    [Required]
    public Guid StorageOperationId { get; init; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; init; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "TransitionInDays must be greater than 0")]
    public int TransitionInDays { get; init; }
    
    [Range(1, int.MaxValue, ErrorMessage = "ExpirationInDays must be greater than 0")]
    public int? ExpirationInDays { get; init; }
}

public record StorageOperationStoringPolicyResponse
{
    public Guid Id { get; init; }
    public Guid StorageOperationId { get; init; }
    public string Name { get; init; }
    public int TransitionInDays { get; init; }
    public int? ExpirationInDays { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
