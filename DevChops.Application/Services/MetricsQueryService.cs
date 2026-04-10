using DevChops.Application.DTOs;
using DevChops.Application.Interfaces;
using DevChops.Domain.Entities;
using DevChops.Domain.ValueObjects;

namespace DevChops.Application.Services;

public class MetricsQueryService(IMetricsRepository metricsRepository)
{
    public async Task<IReadOnlyList<MetricSeriesDto>> GetMetricsForPlansAsync(
        IReadOnlyList<AppServicePlan> plans,
        MetricDefinition metric,
        string aggregation,
        TimeRange timeRange,
        CancellationToken ct = default)
    {
        var tasks = plans.Select(async plan =>
        {
            var series = await metricsRepository.GetMetricAsync(
                plan.Id, metric.MetricName, aggregation, timeRange, ct);

            return new MetricSeriesDto(
                MetricName: series.MetricName,
                DisplayName: metric.DisplayName,
                Unit: metric.Unit,
                ResourceId: plan.Id,
                ResourceName: plan.Name,
                DataPoints: series.DataPoints);
        });

        return await Task.WhenAll(tasks);
    }
}
