namespace DevChops.Domain.Entities;

public class SavedFilter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AppInsightsResourceId { get; set; }
    public List<string> SeverityFilter { get; set; } = [];
    public string? TimeRangePreset { get; set; }
    public string? FreeText { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
