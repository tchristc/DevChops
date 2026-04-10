namespace DevChops.Domain.Entities;

public class UserPreference
{
    public string UserId { get; set; } = string.Empty;
    public string? DefaultSubscriptionId { get; set; }
    public string? DefaultResourceGroupId { get; set; }
    public string DefaultTimeRangePreset { get; set; } = "1h";
}
