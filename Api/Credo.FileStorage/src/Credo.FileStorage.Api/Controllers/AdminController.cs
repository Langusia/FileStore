using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using Credo.Core.Shared.Abstractions;
using Credo.Core.Shared.Extensions;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Messages;
using Credo.FileStorage.Api.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Credo.FileStorage.Api.Controllers;

[Route("api/v1/admin")]
[TypeFilter(typeof(RequiredHeaderFilter), IsReusable = true, Order = 1)]
public sealed class AdminController : ApiController
{
    private readonly IChannelsAdminRepository _channels;
    private readonly IOperationsAdminRepository _operations;
    private readonly IChannelOperationBucketsRepository _buckets;

    public AdminController(
        ISender sender,
        IChannelsAdminRepository channels,
        IOperationsAdminRepository operations,
        IChannelOperationBucketsRepository buckets) : base(sender)
    {
        _channels = channels;
        _operations = operations;
        _buckets = buckets;
    }

    // Channels 
    [HttpGet("channels")]
    public async Task<ActionResult<ApiServiceResponse<IEnumerable<ChannelAdmin>>>> GetChannels(CancellationToken ct)
        => await Result.Create(true)
            .Bind(async _ => Result.Success<IEnumerable<ChannelAdmin>>(await _channels.GetAll(ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<IEnumerable<ChannelAdmin>>(s)),
                HandleFailure<IEnumerable<ChannelAdmin>>
            );

    [HttpPost("channels")]
    public async Task<ActionResult<ApiServiceResponse<ChannelAdmin>>> CreateChannel([FromBody] ChannelAdmin channel, CancellationToken ct)
        => await Result.Create(channel)
            .Bind(async ch => Result.Success(await _channels.Create(ch, ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<ChannelAdmin>(s)),
                HandleFailure<ChannelAdmin>
            );

    [HttpPut("channels/{id:guid}")]
    public async Task<ActionResult<ApiServiceResponse<ChannelAdmin>>> UpdateChannel(Guid id, [FromBody] ChannelAdmin channel, CancellationToken ct)
    {
        if (id != channel.Id) return BadRequest(new BadRequestApiServiceResponse<ChannelAdmin>("Mismatched id"));
        return await Result.Create(channel)
            .Bind(async ch => Result.Success(await _channels.Update(ch, ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<ChannelAdmin>(s)),
                HandleFailure<ChannelAdmin>
            );
    }

    [HttpDelete("channels/{id:guid}")]
    public async Task<ActionResult<ApiServiceResponse<string>>> DeleteChannel(Guid id, CancellationToken ct)
        => await Result.Create(id)
            .Bind(async i => { await _channels.Delete(i, ct); return Result.Success("Deleted"); })
            .Match(
                s => Ok(new SuccessApiServiceResponse<string>(s)),
                HandleFailure<string>
            );

    // Operations
    [HttpGet("operations")]
    public async Task<ActionResult<ApiServiceResponse<IEnumerable<OperationAdmin>>>> GetOperations(CancellationToken ct)
        => await Result.Create(true)
            .Bind(async _ => Result.Success<IEnumerable<OperationAdmin>>(await _operations.GetAll(ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<IEnumerable<OperationAdmin>>(s)),
                HandleFailure<IEnumerable<OperationAdmin>>
            );

    [HttpPost("operations")]
    public async Task<ActionResult<ApiServiceResponse<OperationAdmin>>> CreateOperation([FromBody] OperationAdmin operation, CancellationToken ct)
        => await Result.Create(operation)
            .Bind(async op => Result.Success(await _operations.Create(op, ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<OperationAdmin>(s)),
                HandleFailure<OperationAdmin>
            );

    [HttpPut("operations/{id:guid}")]
    public async Task<ActionResult<ApiServiceResponse<OperationAdmin>>> UpdateOperation(Guid id, [FromBody] OperationAdmin operation, CancellationToken ct)
    {
        if (id != operation.Id) return BadRequest(new BadRequestApiServiceResponse<OperationAdmin>("Mismatched id"));
        return await Result.Create(operation)
            .Bind(async op => Result.Success(await _operations.Update(op, ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<OperationAdmin>(s)),
                HandleFailure<OperationAdmin>
            );
    }

    [HttpDelete("operations/{id:guid}")]
    public async Task<ActionResult<ApiServiceResponse<string>>> DeleteOperation(Guid id, CancellationToken ct)
        => await Result.Create(id)
            .Bind(async i => { await _operations.Delete(i, ct); return Result.Success("Deleted"); })
            .Match(
                s => Ok(new SuccessApiServiceResponse<string>(s)),
                HandleFailure<string>
            );

    // OperationChannels (buckets)
    [HttpGet("operation-buckets")]
    public async Task<ActionResult<ApiServiceResponse<IEnumerable<ChannelOperationBucket>>>> GetBindings(CancellationToken ct)
        => await Result.Create(true)
            .Bind(async _ => Result.Success<IEnumerable<ChannelOperationBucket>>(await _buckets.GetAll(ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<IEnumerable<ChannelOperationBucket>>(s)),
                HandleFailure<IEnumerable<ChannelOperationBucket>>
            );

    public sealed record CreateBindingRequest(Guid ChannelId, Guid OperationId, Guid BucketId);

    [HttpPost("operation-buckets")]
    public async Task<ActionResult<ApiServiceResponse<ChannelOperationBucket>>> CreateBinding([FromBody] CreateBindingRequest req, CancellationToken ct)
        => await Result.Create(req)
            .Bind(async r => Result.Success(await _buckets.Create(new ChannelOperationBucket { ChannelId = r.ChannelId, OperationId = r.OperationId, BucketId = r.BucketId }, ct)))
            .Match(
                s => Ok(new SuccessApiServiceResponse<ChannelOperationBucket>(s)),
                HandleFailure<ChannelOperationBucket>
            );

    [HttpDelete("operation-buckets/{id:guid}")]
    public async Task<ActionResult<ApiServiceResponse<string>>> DeleteBinding(Guid id, CancellationToken ct)
        => await Result.Create(id)
            .Bind(async i => { await _buckets.Delete(i, ct); return Result.Success("Deleted"); })
            .Match(
                s => Ok(new SuccessApiServiceResponse<string>(s)),
                HandleFailure<string>
            );
}




