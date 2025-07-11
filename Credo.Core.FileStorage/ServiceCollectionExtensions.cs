using Microsoft.Extensions.DependencyInjection;
using System.Data;
using Credo.Core.FileStorage.Storage;
using Credo.Core.Minio.DI;
using Credo.Core.Minio.Storage;
using Microsoft.Data.SqlClient;

namespace Credo.Core.FileStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, string connectionString, CredoMinioStorageConfiguration configuration)
    {
        services.AddMinioStorage(configuration);

        services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IFileStorage, FileStorage1>(sp =>
        {
            var minioStorage = sp.GetRequiredService<IMinioStorage>();
            Func<UnitOfWork> uowFactory = () => sp.GetRequiredService<UnitOfWork>();
            return new FileStorage1(minioStorage, uowFactory);
        });

        return services;
    }
}