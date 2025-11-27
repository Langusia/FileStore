using FileStore.Core.Enums;
using FileStore.Core.Interfaces;
using FileStore.Infrastructure.Backends;
using FileStore.Infrastructure.Repositories;
using FileStore.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
