using Credo.Core.FileStorage.DB;
using Credo.Core.FileStorage.DB.Repositories;
using Credo.Core.FileStorage.Storage;
using Credo.Core.Minio.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Credo.Core.FileStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, string connectionString, CredoMinioStorageConfiguration configuration)
    {
        services.AddScoped<IObjectStorage, MinioObjectStorage>();
        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<IChannelOperationBucketRepository, ChannelOperationBucketRepository>();
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddMinioStorage(configuration);
        return services;
    }
}