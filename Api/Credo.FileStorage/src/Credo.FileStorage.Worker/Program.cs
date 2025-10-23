using Credo.FileStorage.Worker;
using Credo.JCS.Extension.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddJobService(builder.Configuration);

var host = builder.Build();
host.Run();