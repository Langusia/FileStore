using Credo.Core.FileStorage.DB;
using Credo.Core.FileStorage.DB.Repositories;
using Credo.Core.FileStorage.Storage;
using Credo.Core.FileStorage.Validation;
using Credo.Core.FileStorage.Validation.MimeProbes;
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
        services.AddScoped<IChannelsAdminRepository, ChannelsAdminRepository>();
        services.AddScoped<IOperationsAdminRepository, OperationsAdminRepository>();
        services.AddScoped<IChannelOperationBindingsRepository, ChannelOperationBindingsRepository>();
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddSingleton<IFileTypeProbe, MagicSignatureProbe>(); // 1st: binaries
        services.AddSingleton<IFileTypeProbe, CsvProbe>(); // 2nd: CSV text
        services.AddSingleton<IFileTypeInspector, CompositeFileTypeInspector>();

        services.AddMinioStorage(configuration);
        return services;
    }
}