using DevChops.Domain.Entities;

namespace DevChops.Application.DTOs;

public record CorrelatedOperationDto(
    string OperationId,
    string OperationName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan? Duration,
    int? ResultCode,
    bool HasErrors,
    int EventCount,
    IReadOnlyList<LogEntry> Events);
