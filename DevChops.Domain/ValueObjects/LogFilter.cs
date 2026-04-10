namespace DevChops.Domain.ValueObjects;

public record LogFilter(
    TimeRange TimeRange,
    IReadOnlyList<string>? SeverityLevels = null,
    string? FreeText = null,
    int? ResultCode = null,
    int MaxResults = 500);
