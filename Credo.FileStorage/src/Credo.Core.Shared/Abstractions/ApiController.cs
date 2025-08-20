using System.Net;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Messages;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Credo.Core.Shared.Abstractions;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected readonly ISender Sender;

    protected ApiController(ISender sender) => Sender = sender;

    protected ActionResult<ApiServiceResponse<T>> HandleFailure<T>(Result result) =>
        result switch
        {
            { IsSuccess: true } => throw new InvalidOperationException(),
            { IsSuccess: false, Errors: var errors } =>
                errors[0].ErrorType switch
                {
                    ErrorTypeEnum.UnprocessableEntity =>
                        new BadRequestObjectResult(
                            new BadRequestApiServiceResponse<T>()
                            {
                                ValidationErrors = errors.Select(error => $"{error.Code} {error.Message}").ToList()
                            }
                        ),
                    ErrorTypeEnum.NoContent => new NoContentResult(),
                    ErrorTypeEnum.NotFound =>
                        new NotFoundObjectResult(
                            new NotFoundApiServiceResponse<T>()
                            {
                                Message = string.Join(',', errors.Select(error => $"{error.Code} - {error.Message}"))
                            }
                        ),
                    ErrorTypeEnum.BadRequest =>
                        new BadRequestObjectResult(
                            new BadRequestApiServiceResponse<T>()
                            {
                                Message = string.Join(',', errors.Select(error => $"{error.Code} - {error.Message}"))
                            }
                        ),
                    ErrorTypeEnum.InternalServerError =>
                        new ObjectResult(
                            new InternalServiceFailedApiServiceResponse()
                            {
                                Message = string.Join(',', errors.Select(error => $"{error.Code} - {error.Message}"))
                            }
                        )
                        {
                            StatusCode = 500
                        },
                    _ =>
                        new BadRequestObjectResult(
                            new BadRequestApiServiceResponse<T>()
                            {
                                Message = string.Join(',', errors.Select(error => $"{error.Code} - {error.Message}"))
                            }
                        ),
                },
            _ =>
                new BadRequestObjectResult(
                    new BadRequestApiServiceResponse<T>()
                )
        };
}