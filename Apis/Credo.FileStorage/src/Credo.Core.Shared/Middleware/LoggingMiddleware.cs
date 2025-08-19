using Credo.Core.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Credo.Core.Shared.Middleware;

public sealed class LoggingMiddleware(
    RequestDelegate next,
    ILogger<LoggingMiddleware> logger
)
{
    private const string ConversationId = "ConversationId";

    public async Task Invoke(HttpContext context)
    {
        var state = new Dictionary<string, object>();
        var headers = context.Request.Headers;
        AddIfNotNull(ref state, ConversationId, ref headers, HeaderConstants.ConversationIdHeaderName);

        using (logger.BeginScope(state))
            await next(context);
    }

    private static void AddIfNotNull(ref Dictionary<string, object> dic, string logKey, ref IHeaderDictionary headers, string headerKey)
    {
        if (headers.TryGetValue(headerKey, out var value) && !StringValues.IsNullOrEmpty(value))
            dic.TryAdd(logKey, value.ToString());
    }
}