using Credo.Core.Minio.Storage;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace Credo.Core.Minio.DI;

public static class StorageBuilderExtensions
{
    public static void AddMinioStorage(this IServiceCollection services, CredoMinioStorageConfiguration configuration)
    {
        services.AddScoped<IMinioStorage, MinioStorage>();
        services.AddMinio(configureClient =>
        {
            if (string.IsNullOrWhiteSpace(configuration.Endpoint))
                throw new ArgumentException("MinioEndpoint is required.", nameof(configuration.Endpoint));
            if (string.IsNullOrWhiteSpace(configuration.AccessKey))
                throw new ArgumentException("AccessKey is required.", nameof(configuration.AccessKey));
            if (string.IsNullOrWhiteSpace(configuration.SecretKey))
                throw new ArgumentException("SecretKey is required.", nameof(configuration.SecretKey));

            configureClient
                .WithEndpoint(configuration.Endpoint, configuration.Port)
                .WithCredentials(configuration.AccessKey, configuration.SecretKey)
                .WithSSL()
                .SetTraceOn();
        });
    }
}