using Credo.JCS.Extension.Services;
using MediatR;

namespace Credo.FileStorage.Worker;

public class Worker(
    ISender sender,
    ILogger<Worker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await JobService.HandleJob(
            async () =>
            {
                logger.LogInformation("Service started at {DateTime}", DateTime.Now);
                logger.LogInformation("Service ended");
            },
            stoppingToken
        ); 
    }
}