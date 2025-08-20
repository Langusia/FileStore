using Credo.Core.FileStorage.V1.DB;
using Credo.Core.FileStorage.V1.DB.Repositories;
using Credo.Core.FileStorage.V1.Storage;
using Credo.Core.Minio.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Credo.Core.FileStorage.V1;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage1(this IServiceCollection services, string connectionString, CredoMinioStorageConfiguration configuration)
    {
        services.AddScoped<IObjectStorage, MinioObjectStorage>();
        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<IChannelOperationBucketRepository, ChannelOperationBucketRepository>();
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        return services;
    }
}