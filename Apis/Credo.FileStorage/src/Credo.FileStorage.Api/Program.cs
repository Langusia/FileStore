using Credo.FileStorage.Api.Configuration;
using Credo.Core.Shared.Middleware;
using Credo.FileStorage.Api.Swagger;
using Credo.FileStorage.Application;
using Microsoft.OpenApi.Models;
using Serilog;
using Credo.Core.FileStorage;
using Credo.Core.Minio.DI;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
    .ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddEnvironmentVariables());

builder.Services.AddControllers();
builder.Services.AddSettings(builder.Configuration);
builder.Services.AddHealthChecks(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddHeaderPropagation();

builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

// SDK Facade wiring
builder.Services.AddFileStorage(
    builder.Configuration.GetConnectionString("DefaultConnection")!,
    new CredoMinioStorageConfiguration
    {
        Endpoint = builder.Configuration.GetValue<string>("Minio:Endpoint") ?? "s3.minio.credo.ge",
        AccessKey = builder.Configuration.GetValue<string>("Minio:AccessKey") ?? "minioadmin",
        SecretKey = builder.Configuration.GetValue<string>("Minio:SecretKey") ?? "securepassword123!"
    }
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Credo.FileStorage.Api API V1", Version = "v1" });
    c.OperationFilter<HeaderFilter>();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/hc-lb");

app.UseMiddleware(typeof(LoggingMiddleware));
app.UseMiddleware(typeof(ErrorHandlingMiddleware));

app.UseAuthorization();
app.UseResponseCompression();
app.MapControllers();
Log.Information("Finished Configuration");

app.Run();