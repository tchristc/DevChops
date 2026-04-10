namespace DevChops.Domain.ValueObjects;

public record MetricDefinition(
    string MetricName,
    string DisplayName,
    string Unit,
    IReadOnlyList<string> SupportedAggregations)
{
    public static readonly IReadOnlyList<MetricDefinition> Catalog =
    [
        new("CpuPercentage",       "CPU %",              "Percent", ["Average", "Maximum"]),
        new("MemoryPercentage",    "Memory %",           "Percent", ["Average", "Maximum"]),
        new("HttpQueueLength",     "HTTP Queue Length",  "Count",   ["Average", "Maximum"]),
        new("DiskQueueLength",     "Disk Queue Length",  "Count",   ["Average", "Maximum"]),
        new("Requests",            "Requests/sec",       "Count",   ["Total"]),
        new("AverageResponseTime", "Avg Response Time",  "Seconds", ["Average"]),
    ];
}
