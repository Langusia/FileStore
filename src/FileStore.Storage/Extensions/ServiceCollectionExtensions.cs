using Amazon.S3;
using FileStore.Storage.Brokers;
using FileStore.Storage.Data;
using FileStore.Storage.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileStore.Storage.Extensions;

/// <summary>
/// Extension methods for configuring FileStore services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds FileStore services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration containing connection strings and S3 settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFileStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add database context
        var connectionString = configuration.GetConnectionString("FileStore")
            ?? throw new InvalidOperationException("FileStore connection string is not configured.");

        services.AddDbContext<FileStoreDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Add S3 client
        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.AddAWSService<IAmazonS3>();

        // Add FileStore services
        services.AddScoped<IObjectStorageBroker, S3ObjectStorageBroker>(sp =>
        {
            var s3Client = sp.GetRequiredService<IAmazonS3>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<S3ObjectStorageBroker>>();
            var serviceUrl = configuration["AWS:ServiceURL"];
            return new S3ObjectStorageBroker(s3Client, logger, serviceUrl);
        });

        services.AddScoped<INamingStrategy, DefaultNamingStrategy>();
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        return services;
    }

    /// <summary>
    /// Adds FileStore services with custom S3 configuration.
    /// </summary>
    public static IServiceCollection AddFileStore(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AmazonS3Config> configureS3)
    {
        // Add database context
        var connectionString = configuration.GetConnectionString("FileStore")
            ?? throw new InvalidOperationException("FileStore connection string is not configured.");

        services.AddDbContext<FileStoreDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Add S3 client with custom configuration
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var config = new AmazonS3Config();
            configureS3(config);
            return new AmazonS3Client(config);
        });

        // Add FileStore services
        services.AddScoped<IObjectStorageBroker, S3ObjectStorageBroker>(sp =>
        {
            var s3Client = sp.GetRequiredService<IAmazonS3>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<S3ObjectStorageBroker>>();
            var serviceUrl = configuration["AWS:ServiceURL"];
            return new S3ObjectStorageBroker(s3Client, logger, serviceUrl);
        });

        services.AddScoped<INamingStrategy, DefaultNamingStrategy>();
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        return services;
    }
}
