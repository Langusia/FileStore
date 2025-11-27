using FileStore.Core.Enums;
using FileStore.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileStore.Infrastructure.Services;

public class TieringBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TieringOptions _options;
    private readonly ILogger<TieringBackgroundService> _logger;

    public TieringBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<TieringOptions> options,
        ILogger<TieringBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Tiering service is disabled");
            return;
        }

        _logger.LogInformation("Tiering service started with interval {Interval} minutes", _options.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
                await ProcessTieringAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Tiering service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tiering service");
            }
        }
    }

    private async Task ProcessTieringAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var objectRepository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
        var backend = scope.ServiceProvider.GetRequiredService<IFileStorageBackend>();

        var excludedBuckets = _options.BucketsExcludedFromCold?.ToList() ?? new List<string>();

        _logger.LogInformation("Starting tiering process: moving objects older than {Days} days to cold storage", _options.ColdAfterDays);

        var processedCount = 0;
        var errorCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var objects = await objectRepository.GetObjectsForTieringAsync(
                _options.ColdAfterDays,
                excludedBuckets,
                _options.BatchSize,
                cancellationToken);

            if (!objects.Any())
                break;

            foreach (var obj in objects)
            {
                try
                {
                    _logger.LogDebug("Moving object {ObjectId} from Hot to Cold tier", obj.ObjectId);

                    await backend.MoveTierAsync(obj.RelativePath, StorageTier.Hot, StorageTier.Cold, cancellationToken);
                    await objectRepository.UpdateTierAsync(obj.ObjectId, StorageTier.Cold, cancellationToken);

                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to tier object {ObjectId}", obj.ObjectId);
                    errorCount++;
                }
            }

            if (objects.Count < _options.BatchSize)
                break;
        }

        _logger.LogInformation("Tiering process completed: {Processed} objects moved to cold storage, {Errors} errors", processedCount, errorCount);
    }
}

public class TieringOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public int ColdAfterDays { get; set; } = 365;
    public int BatchSize { get; set; } = 100;
    public List<string>? BucketsExcludedFromCold { get; set; }
}
