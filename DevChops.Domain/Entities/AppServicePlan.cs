namespace DevChops.Domain.Entities;

public record AppServicePlan(
    string Id,
    string Name,
    string ResourceGroupName,
    string SubscriptionId,
    string Sku,
    string Location,
    int NumberOfWorkers);
