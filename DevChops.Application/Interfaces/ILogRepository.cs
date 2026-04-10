using DevChops.Domain.Entities;
using DevChops.Domain.ValueObjects;

namespace DevChops.Application.Interfaces;

public interface ILogRepository
{
    Task<IReadOnlyList<LogEntry>> GetCorrelatedLogsAsync(
        string appInsightsResourceId,
        LogFilter filter,
        CancellationToken ct = default);
}
