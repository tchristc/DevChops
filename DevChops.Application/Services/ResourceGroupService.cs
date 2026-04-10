using DevChops.Application.Interfaces;
using DevChops.Domain.Entities;

namespace DevChops.Application.Services;

public class ResourceGroupService(IAzureResourceRepository repository)
{
    private IReadOnlyList<ResourceGroup>? _cachedGroups;
    private IReadOnlyList<AppServicePlan>? _cachedPlans;
    private IReadOnlyList<AppInsightsComponent>? _cachedComponents;

    public async Task<IReadOnlyList<ResourceGroup>> GetResourceGroupsAsync(CancellationToken ct = default)
    {
        _cachedGroups ??= await repository.GetResourceGroupsAsync(ct);
        return _cachedGroups;
    }

    public async Task<IReadOnlyList<AppServicePlan>> GetAppServicePlansAsync(CancellationToken ct = default)
    {
        if (_cachedPlans is not null) return _cachedPlans;

        var groups = await GetResourceGroupsAsync(ct);
        var subscriptionIds = groups.Select(g => g.SubscriptionId).Distinct().ToList();
        var all = new List<AppServicePlan>();

        foreach (var subId in subscriptionIds)
            all.AddRange(await repository.GetAppServicePlansAsync(subId, ct));

        _cachedPlans = all;
        return _cachedPlans;
    }

    public async Task<IReadOnlyList<AppInsightsComponent>> GetAppInsightsComponentsAsync(CancellationToken ct = default)
    {
        if (_cachedComponents is not null) return _cachedComponents;

        var groups = await GetResourceGroupsAsync(ct);
        var subscriptionIds = groups.Select(g => g.SubscriptionId).Distinct().ToList();
        var all = new List<AppInsightsComponent>();

        foreach (var subId in subscriptionIds)
            all.AddRange(await repository.GetAppInsightsComponentsAsync(subId, null, ct));

        _cachedComponents = all;
        return _cachedComponents;
    }

    public void ClearCache()
    {
        _cachedGroups = null;
        _cachedPlans = null;
        _cachedComponents = null;
    }
}
