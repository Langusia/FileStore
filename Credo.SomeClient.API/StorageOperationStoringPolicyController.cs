using Microsoft.AspNetCore.Mvc;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Repositories;
using Credo.SomeClient.API.Dtos;
using System.Data;

namespace Credo.SomeClient.API;

[ApiController]
[Route("api/[controller]")]
public class StorageOperationStoringPolicyController : ControllerBase
{
    private readonly Func<UnitOfWork> _unitOfWorkFactory;

    public StorageOperationStoringPolicyController(Func<UnitOfWork> unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    /// <summary>
    /// Get all storage operation storing policies
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StorageOperationStoringPolicyResponse>>> GetAll(CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkFactory();
        try
        {
            var policies = await uow.StorageOperationStoringPolicyRepository.GetAllAsync();
            var response = policies.Select(MapToResponse);
            uow.Commit();
            return Ok(response);
        }
        catch (Exception ex)
        {
            uow.Rollback();
            return StatusCode(500, new { error = "Failed to retrieve storage operation storing policies", details = ex.Message });
        }
    }

    /// <summary>
    /// Get storage operation storing policy by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StorageOperationStoringPolicyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkFactory();
        try
        {
            var policy = await uow.StorageOperationStoringPolicyRepository.GetByIdAsync(id);
            if (policy == null)
            {
                uow.Rollback();
                return NotFound(new { error = "Storage operation storing policy not found" });
            }

            var response = MapToResponse(policy);
            uow.Commit();
            return Ok(response);
        }
        catch (Exception ex)
        {
            uow.Rollback();
            return StatusCode(500, new { error = "Failed to retrieve storage operation storing policy", details = ex.Message });
        }
    }

    /// <summary>
    /// Get storage operation storing policy by StorageOperationId
    /// </summary>
    [HttpGet("by-storage-operation/{storageOperationId:guid}")]
    public async Task<ActionResult<StorageOperationStoringPolicyResponse>> GetByStorageOperationId(Guid storageOperationId, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkFactory();
        try
        {
            var policy = await uow.StorageOperationStoringPolicyRepository.GetByStorageOperationIdAsync(storageOperationId);
            if (policy == null)
            {
                uow.Rollback();
                return NotFound(new { error = "Storage operation storing policy not found for the specified storage operation" });
            }

            var response = MapToResponse(policy);
            uow.Commit();
            return Ok(response);
        }
        catch (Exception ex)
        {
            uow.Rollback();
            return StatusCode(500, new { error = "Failed to retrieve storage operation storing policy", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new storage operation storing policy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StorageOperationStoringPolicyResponse>> Create(CreateStorageOperationStoringPolicyRequest request, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkFactory();
        try
        {
            // Validate that the storage operation exists
            var storageOperation = await uow.StorageOperationRepository.GetByIdAsync(request.StorageOperationId);
            if (storageOperation == null)
            {
                uow.Rollback();
                return BadRequest(new { error = "Storage operation not found" });
            }

            // Check if a policy already exists for this storage operation
            var existingPolicy = await uow.StorageOperationStoringPolicyRepository.GetByStorageOperationIdAsync(request.StorageOperationId);
            if (existingPolicy != null)
            {
                uow.Rollback();
                return BadRequest(new { error = "A storage operation storing policy already exists for this storage operation" });
            }

            var policy = new StorageOperationStoringPolicy(
                Guid.NewGuid(),
                request.StorageOperationId,
                request.Name,
                request.TransitionInDays,
                request.ExpirationInDays
            );

            await uow.StorageOperationStoringPolicyRepository.CreateAsync(policy);
            uow.Commit();

            var response = MapToResponse(policy);
            return CreatedAtAction(nameof(GetById), new { id = policy.Id }, response);
        }
        catch (Exception ex)
        {
            uow.Rollback();
            return StatusCode(500, new { error = "Failed to create storage operation storing policy", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing storage operation storing policy
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StorageOperationStoringPolicyResponse>> Update(Guid id, UpdateStorageOperationStoringPolicyRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
        }

        using var uow = _unitOfWorkFactory();
        try
        {
            // Check if the policy exists
            var existingPolicy = await uow.StorageOperationStoringPolicyRepository.GetByIdAsync(id);
            if (existingPolicy == null)
            {
                uow.Rollback();
                return NotFound(new { error = "Storage operation storing policy not found" });
            }

            // Validate that the storage operation exists
            var storageOperation = await uow.StorageOperationRepository.GetByIdAsync(request.StorageOperationId);
            if (storageOperation == null)
            {
                uow.Rollback();
                return BadRequest(new { error = "Storage operation not found" });
            }

            // Check if another policy already exists for this storage operation (excluding current one)
            var conflictingPolicy = await uow.StorageOperationStoringPolicyRepository.GetByStorageOperationIdAsync(request.StorageOperationId);
            if (conflictingPolicy != null && conflictingPolicy.Id != id)
            {
                uow.Rollback();
                return BadRequest(new { error = "Another storage operation storing policy already exists for this storage operation" });
            }

            var updatedPolicy = new StorageOperationStoringPolicy(
                id,
                request.StorageOperationId,
                request.Name,
                request.TransitionInDays,
                request.ExpirationInDays
            );

            await uow.StorageOperationStoringPolicyRepository.UpdateAsync(updatedPolicy);
            uow.Commit();

            var response = MapToResponse(updatedPolicy);
            return Ok(response);
        }
        catch (Exception ex)
        {
            uow.Rollback();
            return StatusCode(500, new { error = "Failed to update storage operation storing policy", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a storage operation storing policy
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        using var uow = _unitOfWorkFactory();
        try
        {
            // Check if the policy exists
            var existingPolicy = await uow.StorageOperationStoringPolicyRepository.GetByIdAsync(id);
            if (existingPolicy == null)
            {
                uow.Rollback();
                return NotFound(new { error = "Storage operation storing policy not found" });
            }

            await uow.StorageOperationStoringPolicyRepository.DeleteAsync(id);
            uow.Commit();

            return NoContent();
        }
        catch (Exception ex)
        {
            uow.Rollback();
            return StatusCode(500, new { error = "Failed to delete storage operation storing policy", details = ex.Message });
        }
    }

    private static StorageOperationStoringPolicyResponse MapToResponse(StorageOperationStoringPolicy policy)
    {
        return new StorageOperationStoringPolicyResponse
        {
            Id = policy.Id,
            StorageOperationId = policy.StorageOperationId,
            Name = policy.Name,
            TransitionInDays = policy.TransitionInDays,
            ExpirationInDays = policy.ExpirationInDays,
            CreatedAt = DateTime.UtcNow, // Note: This would ideally come from the database
            UpdatedAt = DateTime.UtcNow  // Note: This would ideally come from the database
        };
    }
}
