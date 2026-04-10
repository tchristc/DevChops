using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using DevChops.Application.Interfaces;
using DevChops.Domain.Entities;
using DevChops.Domain.ValueObjects;

namespace DevChops.Infrastructure.Azure;

public class AzureMetricsRepository(IAzureCredentialProvider credentialProvider) : IMetricsRepository
{
    public async Task<MetricSeries> GetMetricAsync(
        string resourceId,
        string metricName,
        string aggregation,
        TimeRange range,
        CancellationToken ct = default)
    {
        var client = new MetricsQueryClient(credentialProvider.GetCredential());

        var options = new MetricsQueryOptions
        {
            TimeRange   = new QueryTimeRange(range.Start, range.End),
            Granularity = range.AutoGranularity,
        };
        options.Aggregations.Add(ParseAggregation(aggregation));

        var response = await client.QueryResourceAsync(
            resourceId, [metricName], options, ct);

        var metric = response.Value.Metrics.FirstOrDefault();
        if (metric is null)
            return new MetricSeries(metricName, resourceId, string.Empty, []);

        var dataPoints = metric.TimeSeries
            .SelectMany(ts => ts.Values)
            .Select(v => new MetricDataPoint(
                v.TimeStamp,
                v.Average,
                v.Maximum,
                v.Minimum,
                v.Total))
            .OrderBy(dp => dp.Timestamp)
            .ToList();

        return new MetricSeries(metricName, resourceId, metric.Unit.ToString(), dataPoints);
    }

    private static MetricAggregationType ParseAggregation(string aggregation) =>
        aggregation.ToLowerInvariant() switch
        {
            "average" => MetricAggregationType.Average,
            "maximum" => MetricAggregationType.Maximum,
            "minimum" => MetricAggregationType.Minimum,
            "total"   => MetricAggregationType.Total,
            _         => MetricAggregationType.Average,
        };
}
