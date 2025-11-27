using System.Reflection;
using FileStore.Core.Enums;
using FileStore.Core.Interfaces;
using FileStore.Infrastructure.Backends;
using FileStore.Infrastructure.Repositories;
using FileStore.Infrastructure.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "FileStore API",
        Description = "S3-like internal object storage service with pluggable backends",
        Contact = new OpenApiContact
        {
            Name = "FileStore Team",
            Email = "filestore@company.com"
        },
        License = new OpenApiLicense
        {
            Name = "Internal Use Only"
        }
    });

    // Enable XML comments for better documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add examples and schemas
    options.EnableAnnotations();

    // Add custom operation IDs for better client generation
    options.CustomOperationIds(apiDesc =>
    {
        return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null;
    });

    // Add server information
    options.AddServer(new OpenApiServer
    {
        Url = "http://localhost:5000",
        Description = "Development Server"
    });

    options.AddServer(new OpenApiServer
    {
        Url = "https://filestore.company.com",
        Description = "Production Server"
    });
});

var storageBackend = builder.Configuration.GetValue<BackendType>("Storage:Backend");

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("Database"));

builder.Services.Configure<SmbStorageOptions>(options =>
{
    options.HotRootPath = builder.Configuration["Storage:HotRootPath"] ?? "/mnt/storage/hot";
    options.ColdRootPath = builder.Configuration["Storage:ColdRootPath"] ?? "/mnt/storage/cold";
});

builder.Services.Configure<StorageServiceOptions>(options =>
{
    var shardSection = builder.Configuration.GetSection("Storage:Shard");
    options.Shard.Levels = shardSection.GetValue<int>("Levels", 3);
    options.Shard.CharsPerShard = shardSection.GetValue<int>("CharsPerShard", 2);
    options.MaxFileSizeMb = builder.Configuration.GetValue<int>("Storage:MaxFileSizeMb", 100);

    var allowedTypes = builder.Configuration.GetSection("Storage:AllowedContentTypes").Get<List<string>>();
    options.AllowedContentTypes = allowedTypes;
});

builder.Services.Configure<TieringOptions>(options =>
{
    options.Enabled = builder.Configuration.GetValue<bool>("Tiering:Enabled", true);
    options.IntervalMinutes = builder.Configuration.GetValue<int>("Tiering:IntervalMinutes", 60);
    options.ColdAfterDays = builder.Configuration.GetValue<int>("Tiering:ColdAfterDays", 365);
    options.BatchSize = builder.Configuration.GetValue<int>("Tiering:BatchSize", 100);

    var excludedBuckets = builder.Configuration.GetSection("Tiering:BucketsExcludedFromCold").Get<List<string>>();
    options.BucketsExcludedFromCold = excludedBuckets;
});

switch (storageBackend)
{
    case BackendType.SMB:
        builder.Services.AddSingleton<IFileStorageBackend, SmbStorageBackend>();
        break;
    default:
        throw new InvalidOperationException($"Unsupported storage backend: {storageBackend}");
}

builder.Services.AddSingleton<IShardingStrategy, ShardingStrategy>();
builder.Services.AddSingleton<IObjectRepository, ObjectRepository>();
builder.Services.AddSingleton<IObjectLinkRepository, ObjectLinkRepository>();
builder.Services.AddSingleton<IStorageService, StorageService>();

builder.Services.AddHostedService<TieringBackgroundService>();

var app = builder.Build();

// Enable Swagger in all environments (can be restricted with authentication in production)
app.UseSwagger(options =>
{
    options.RouteTemplate = "api-docs/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/api-docs/v1/swagger.json", "FileStore API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "FileStore API Documentation";
    options.DefaultModelsExpandDepth(2);
    options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.ShowExtensions();
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health")
    .Produces<object>(StatusCodes.Status200OK)
    .WithOpenApi(operation => new(operation)
    {
        Summary = "Health Check",
        Description = "Returns the health status of the API service"
    });

app.Run();
