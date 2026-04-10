using DevChops.Domain.Entities;
using DevChops.Domain.ValueObjects;

namespace DevChops.Application.Interfaces;

public interface IMetricsRepository
{
    Task<MetricSeries> GetMetricAsync(
        string resourceId,
        string metricName,
        string aggregation,
        TimeRange range,
        CancellationToken ct = default);
}
