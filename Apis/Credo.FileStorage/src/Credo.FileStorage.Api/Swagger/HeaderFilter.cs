using Credo.Core.Shared.Constants;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Credo.FileStorage.Api.Swagger;

public class HeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderConstants.ConversationIdHeaderName,
            Description = "Conversation id",
            In = ParameterLocation.Header,
            Schema = new OpenApiSchema { Type = "string" },
            Required = true,
            Example = new OpenApiString(Guid.NewGuid().ToString())
        });
    }
}