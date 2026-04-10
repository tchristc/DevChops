using DevChops.Domain.Entities;

namespace DevChops.Application.DTOs;

public record MetricSeriesDto(
    string MetricName,
    string DisplayName,
    string Unit,
    string ResourceId,
    string ResourceName,
    IReadOnlyList<MetricDataPoint> DataPoints);
