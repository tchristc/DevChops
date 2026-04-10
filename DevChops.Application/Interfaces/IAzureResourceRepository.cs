using DevChops.Domain.Entities;

namespace DevChops.Application.Interfaces;

public interface IAzureResourceRepository
{
    Task<IReadOnlyList<ResourceGroup>> GetResourceGroupsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AppServicePlan>> GetAppServicePlansAsync(
        string subscriptionId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AppInsightsComponent>> GetAppInsightsComponentsAsync(
        string subscriptionId,
        string? resourceGroupName = null,
        CancellationToken ct = default);
}
