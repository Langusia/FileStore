using Credo.FileStorage.Application;
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

var host = builder.Build();
host.Run();