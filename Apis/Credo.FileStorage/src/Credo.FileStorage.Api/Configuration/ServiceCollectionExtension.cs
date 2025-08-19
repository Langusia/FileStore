using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Settings;
using Credo.FileStorage.Persistence;
using Credo.Core.FileStorage;
using Credo.Core.Minio.DI;

namespace Credo.FileStorage.Api.Configuration;

internal static class ServiceCollectionExtension
{
    public static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks();
        /*
           .AddCheck("CredoBnkDB-check", new SqlConnectionHealthCheck(configuration.GetConnectionString("CredoBnk")), HealthStatus.Unhealthy, new string[] { "CredoBnk", "Database" })
           .AddUrlGroup(new Uri(configuration.GetSection("ExternalServiceSettings")["PTBridgeUrl"]), name: "PTBridgeService-check", tags: new string[] { "PTBridge", "Service" });
        */
    }

    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITodoQueryRepository, TodoQueryRepository>();
        services.AddScoped<ITodoCommandRepository, TodoCommandRepository>();
    }

    public static void AddServices(this IServiceCollection services)
    {
        // Facade SDK wiring will be configured in Program via AddFileStorage, keeping this hook for domain services
    }

    public static void AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConnectionStrings>(configuration.GetSection("ConnectionStrings"));
    }
}