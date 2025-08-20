using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Settings;
using Credo.FileStorage.Persistence;

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
        services.AddScoped<IChannelsAdminRepository, ChannelsAdminRepository>();
        services.AddScoped<IOperationsAdminRepository, OperationsAdminRepository>();
        services.AddScoped<IChannelOperationBucketsRepository, ChannelOperationBucketsRepository>();
    }

    public static void AddServices(this IServiceCollection services)
    {
    }

    public static void AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConnectionStrings>(configuration.GetSection("ConnectionStrings"));
    }
}