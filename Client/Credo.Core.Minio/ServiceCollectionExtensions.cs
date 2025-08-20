using Credo.Core.Minio.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Credo.Core.Minio;

public static class ServiceCollectionExtensions
{
    public static void AddCredoCoreMinio(this IServiceCollection services)
    {
        services.AddScoped<IMinioStorage, MinioStorage>();
    }
}