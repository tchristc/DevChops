using DevChops.Application.Interfaces;
using DevChops.Application.Services;
using DevChops.Infrastructure.Azure;
using DevChops.Infrastructure.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace DevChops.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Azure repositories — scoped so each Blazor Server circuit gets its own instance
        // with the user's token credential (IAzureCredentialProvider registered by the host)
        services.AddScoped<IAzureResourceRepository, AzureResourceRepository>();
        services.AddScoped<ILogRepository, AzureLogRepository>();
        services.AddScoped<IMetricsRepository, AzureMetricsRepository>();

        // In-memory services — singleton, keyed internally by user ID
        services.AddSingleton<IUserPreferenceService, InMemoryUserPreferenceService>();
        services.AddSingleton<ISavedFilterService, InMemorySavedFilterService>();

        // Application services — scoped (caches resource lists per user session)
        services.AddScoped<ResourceGroupService>();
        services.AddScoped<LogQueryService>();
        services.AddScoped<MetricsQueryService>();

        return services;
    }
}
