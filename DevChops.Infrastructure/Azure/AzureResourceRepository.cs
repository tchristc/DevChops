using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using DevChops.Application.Interfaces;
using DevChops.Domain.Entities;

namespace DevChops.Infrastructure.Azure;

public class AzureResourceRepository(IAzureCredentialProvider credentialProvider) : IAzureResourceRepository
{
    public async Task<IReadOnlyList<ResourceGroup>> GetResourceGroupsAsync(CancellationToken ct = default)
    {
        var armClient = new ArmClient(credentialProvider.GetCredential());
        var result = new List<ResourceGroup>();

        await foreach (var sub in armClient.GetSubscriptions().GetAllAsync(ct))
        await foreach (var rg in sub.GetResourceGroups().GetAllAsync(cancellationToken: ct))
        {
            result.Add(new ResourceGroup(
                rg.Id!.ToString(),
                rg.Data.Name,
                sub.Data.SubscriptionId!,
                rg.Data.Location.ToString()));
        }

        return result;
    }

    public async Task<IReadOnlyList<AppServicePlan>> GetAppServicePlansAsync(
        string subscriptionId, CancellationToken ct = default)
    {
        var armClient = new ArmClient(credentialProvider.GetCredential());
        var sub = armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        var result = new List<AppServicePlan>();

        await foreach (var plan in sub.GetAppServicePlansAsync(cancellationToken: ct))
        {
            result.Add(new AppServicePlan(
                plan.Id!.ToString(),
                plan.Data.Name,
                plan.Id.ResourceGroupName,
                subscriptionId,
                plan.Data.Sku?.Name ?? "Unknown",
                plan.Data.Location.ToString(),
                plan.Data.MaximumNumberOfWorkers ?? 0));
        }

        return result;
    }

    public async Task<IReadOnlyList<AppInsightsComponent>> GetAppInsightsComponentsAsync(
        string subscriptionId, string? resourceGroupName = null, CancellationToken ct = default)
    {
        var armClient = new ArmClient(credentialProvider.GetCredential());
        var sub = armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        var result = new List<AppInsightsComponent>();

        string filter = "resourceType eq 'microsoft.insights/components'";
        if (resourceGroupName is not null)
            filter += $" and resourceGroup eq '{resourceGroupName}'";

        await foreach (var resource in sub.GetGenericResourcesAsync(filter: filter, cancellationToken: ct))
        {
            result.Add(new AppInsightsComponent(
                resource.Id!.ToString(),
                resource.Data.Name,
                resource.Id.ResourceGroupName,
                subscriptionId,
                resource.Data.Location.ToString()));
        }

        return result;
    }
}
