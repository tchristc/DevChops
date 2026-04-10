namespace DevChops.Domain.Entities;

public record AppInsightsComponent(
    string Id,
    string Name,
    string ResourceGroupName,
    string SubscriptionId,
    string Location);
