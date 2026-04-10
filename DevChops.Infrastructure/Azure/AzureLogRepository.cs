using Azure;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using DevChops.Application.Interfaces;
using DevChops.Domain.Entities;
using DevChops.Domain.ValueObjects;

namespace DevChops.Infrastructure.Azure;

public class AzureLogRepository(IAzureCredentialProvider credentialProvider) : ILogRepository
{
    public async Task<IReadOnlyList<LogEntry>> GetCorrelatedLogsAsync(
        string appInsightsResourceId,
        LogFilter filter,
        CancellationToken ct = default)
    {
        var client = new LogsQueryClient(credentialProvider.GetCredential());
        var query = BuildQuery(filter);
        var timeRange = new QueryTimeRange(filter.TimeRange.Start, filter.TimeRange.End);

        try
        {
            var response = await client.QueryResourceAsync(
                new global::Azure.Core.ResourceIdentifier(appInsightsResourceId),
                query,
                timeRange,
                    cancellationToken: ct);

                    return MapRows(response.Value.Table);
                }
                catch (Exception ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                {
                    throw new UnauthorizedAccessException(
                        $"Access denied to Application Insights resource '{appInsightsResourceId}'.", ex);
                }
    }

    private static string BuildQuery(LogFilter filter)
    {
        var parts = new List<string>
        {
            "union requests, exceptions, traces, dependencies",
            "| where isnotempty(operation_Id)",
        };

        if (filter.SeverityLevels is { Count: > 0 })
        {
            var levels = string.Join(", ", filter.SeverityLevels.Select(MapSeverityToInt));
            parts.Add($"| where severityLevel in ({levels}) or itemType == 'request' or itemType == 'dependency'");
        }

        if (!string.IsNullOrWhiteSpace(filter.FreeText))
        {
            var escaped = filter.FreeText.Replace("'", "\\'");
            parts.Add($"| where message has '{escaped}' or name has '{escaped}' or operation_Name has '{escaped}'");
        }

        parts.Add(
            "| project operationId = operation_Id, operationName = operation_Name," +
            " itemType, timestamp, severityLevel = toint(severityLevel)," +
            " resultCode = toint(resultCode), duration = todouble(duration)," +
            " message = coalesce(message, outerMessage, name)," +
            " details = tostring(customDimensions)");
        parts.Add("| order by timestamp desc");
        parts.Add($"| take {filter.MaxResults}");

        return string.Join("\n", parts);
    }

    private static string MapSeverityToInt(string severity) => severity.ToLowerInvariant() switch
    {
        "verbose"     => "0",
        "information" => "1",
        "warning"     => "2",
        "error"       => "3",
        "critical"    => "4",
        _             => "1",
    };

    private static IReadOnlyList<LogEntry> MapRows(LogsTable table)
    {
        var result = new List<LogEntry>();

        int idxOpId        = IndexOf(table, "operationId");
        int idxOpName      = IndexOf(table, "operationName");
        int idxType        = IndexOf(table, "itemType");
        int idxTimestamp   = IndexOf(table, "timestamp");
        int idxSeverity    = IndexOf(table, "severityLevel");
        int idxResultCode  = IndexOf(table, "resultCode");
        int idxDuration    = IndexOf(table, "duration");
        int idxMessage     = IndexOf(table, "message");
        int idxDetails     = IndexOf(table, "details");

        foreach (var row in table.Rows)
        {
            result.Add(new LogEntry(
                OperationId:   row.GetString(idxOpId)      ?? string.Empty,
                OperationName: row.GetString(idxOpName)    ?? string.Empty,
                Type:          row.GetString(idxType)      ?? string.Empty,
                Timestamp:     row.GetDateTimeOffset(idxTimestamp) ?? DateTimeOffset.UtcNow,
                Severity:      MapIntToSeverity(row.GetInt32(idxSeverity)),
                ResultCode:    row.GetInt32(idxResultCode),
                Duration:      row.GetDouble(idxDuration) is double ms
                                   ? TimeSpan.FromMilliseconds(ms) : null,
                Message:       row.GetString(idxMessage),
                Details:       row.GetString(idxDetails)));
        }

        return result;
    }

    private static int IndexOf(LogsTable table, string column)
    {
        for (int i = 0; i < table.Columns.Count; i++)
            if (table.Columns[i].Name == column) return i;
        return -1;
    }

    private static string MapIntToSeverity(int? level) => level switch
    {
        0 => "Verbose",
        1 => "Information",
        2 => "Warning",
        3 => "Error",
        4 => "Critical",
        _ => "Information",
    };
}
