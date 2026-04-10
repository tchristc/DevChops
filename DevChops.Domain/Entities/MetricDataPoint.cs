namespace DevChops.Domain.Entities;

public record MetricDataPoint(
    DateTimeOffset Timestamp,
    double? Average,
    double? Maximum,
    double? Minimum,
    double? Total);
