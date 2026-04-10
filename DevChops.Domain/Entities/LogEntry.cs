namespace DevChops.Domain.Entities;

public record LogEntry(
    string OperationId,
    string OperationName,
    string Type,
    DateTimeOffset Timestamp,
    string Severity,
    int? ResultCode,
    TimeSpan? Duration,
    string? Message,
    string? Details);
