using Credo.FileStorage.Application;
using Credo.Core.FileStorage;
using Credo.Core.Minio.DI;
using Credo.FileStorage.Worker;
using Credo.FileStorage.Worker.Configuration;
using Credo.JCS.Extension.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddJobService(builder.Configuration);
builder.Services.AddSettings(builder.Configuration);
builder.Services.AddHealthChecks(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddRepositories();
builder.Services.AddServices();

// Wire FileStorage library with DB and MinIO configuration
var minioConfig = builder.Configuration.GetSection("Minio").Get<CredoMinioStorageConfiguration>();
var documentDb = builder.Configuration.GetConnectionString("DocumentDb");
builder.Services.AddFileStorage(documentDb!, minioConfig!);

var host = builder.Build();
host.Run();