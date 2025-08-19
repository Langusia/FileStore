using Credo.Core.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Credo.FileStorage.Api.Filters;

public class RequiredHeaderFilter(ILogger<RequiredHeaderFilter> logger) : IAsyncActionFilter
{
    private static readonly HashSet<string> RequiredHeaderNames =
        new(StringComparer.OrdinalIgnoreCase) { HeaderConstants.ConversationIdHeaderName };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasMissingHeaders = !RequiredHeaderNames.IsSubsetOf(context.HttpContext.Request.Headers.Keys);

        if (hasMissingHeaders)
        {
            var missingHeaders = RequiredHeaderNames.Except(
                context.HttpContext.Request.Headers.Keys,
                StringComparer.OrdinalIgnoreCase
            );

            var csHeaders = string.Join(", ", missingHeaders);
            logger.LogError("Missing headers: {MissingHeaders}", csHeaders);
            context.Result = new BadRequestObjectResult($"Missing headers: {csHeaders}");
            return;
        }

        await next();
    }
}