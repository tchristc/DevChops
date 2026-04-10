using DevChops.Application.DTOs;
using DevChops.Application.Interfaces;
using DevChops.Domain.ValueObjects;

namespace DevChops.Application.Services;

public class LogQueryService(ILogRepository logRepository)
{
    public async Task<IReadOnlyList<CorrelatedOperationDto>> GetCorrelatedOperationsAsync(
        string appInsightsResourceId,
        LogFilter filter,
        CancellationToken ct = default)
    {
        var entries = await logRepository.GetCorrelatedLogsAsync(appInsightsResourceId, filter, ct);

        return entries
            .Where(e => !string.IsNullOrEmpty(e.OperationId))
            .GroupBy(e => e.OperationId)
            .Select(g =>
            {
                var events = g.OrderBy(e => e.Timestamp).ToList();
                var requestEvent = events.FirstOrDefault(e => e.Type == "request");
                return new CorrelatedOperationDto(
                    OperationId: g.Key,
                    OperationName: requestEvent?.OperationName ?? events.First().OperationName,
                    StartTime: events.Min(e => e.Timestamp),
                    EndTime: events.Max(e => e.Timestamp),
                    Duration: requestEvent?.Duration
                              ?? events.Max(e => e.Timestamp) - events.Min(e => e.Timestamp),
                    ResultCode: requestEvent?.ResultCode,
                    HasErrors: events.Any(e => e.Type == "exception")
                               || (requestEvent?.ResultCode >= 400),
                    EventCount: events.Count,
                    Events: events);
            })
            .OrderByDescending(op => op.StartTime)
            .ToList();
    }
}
