namespace DevChops.Domain.ValueObjects;

public record TimeRange(DateTimeOffset Start, DateTimeOffset End, string? PresetLabel = null)
{
    public static TimeRange Last15Minutes => new(DateTimeOffset.UtcNow.AddMinutes(-15), DateTimeOffset.UtcNow, "15m");
    public static TimeRange Last1Hour     => new(DateTimeOffset.UtcNow.AddHours(-1),    DateTimeOffset.UtcNow, "1h");
    public static TimeRange Last6Hours    => new(DateTimeOffset.UtcNow.AddHours(-6),    DateTimeOffset.UtcNow, "6h");
    public static TimeRange Last24Hours   => new(DateTimeOffset.UtcNow.AddHours(-24),   DateTimeOffset.UtcNow, "24h");
    public static TimeRange Last7Days     => new(DateTimeOffset.UtcNow.AddDays(-7),     DateTimeOffset.UtcNow, "7d");
    public static TimeRange Last30Days    => new(DateTimeOffset.UtcNow.AddDays(-30),    DateTimeOffset.UtcNow, "30d");

    public static readonly IReadOnlyList<(string Label, string Display, Func<TimeRange> Factory)> Presets =
    [
        ("15m", "15 min",  () => Last15Minutes),
        ("1h",  "1 hour",  () => Last1Hour),
        ("6h",  "6 hours", () => Last6Hours),
        ("24h", "24 hours",() => Last24Hours),
        ("7d",  "7 days",  () => Last7Days),
        ("30d", "30 days", () => Last30Days),
    ];

    public static TimeRange FromPresetKey(string key)
    {
        var preset = Presets.FirstOrDefault(p => p.Label == key);
        return preset != default ? preset.Factory() : Last1Hour;
    }

    public TimeSpan AutoGranularity => (End - Start).TotalHours switch
    {
        <= 1   => TimeSpan.FromMinutes(1),
        <= 6   => TimeSpan.FromMinutes(5),
        <= 24  => TimeSpan.FromMinutes(15),
        <= 168 => TimeSpan.FromHours(1),
        _      => TimeSpan.FromDays(1),
    };
}
