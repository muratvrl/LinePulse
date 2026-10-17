namespace LinePulse.Redis;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public required string Host { get; set; }
    public int Port { get; set; }
    public int CorrelationTtlHours { get; set; }
}
