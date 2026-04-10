namespace DevChops.Domain.Entities;

public record MetricSeries(
    string MetricName,
    string ResourceId,
    string Unit,
    IReadOnlyList<MetricDataPoint> DataPoints);
