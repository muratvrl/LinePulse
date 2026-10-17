namespace LinePulse.RabbitMq;

public class Settings
{
    public const string SectionName = "RabbitMq";

    public required string Host { get; set; }
    public int Port { get; set; }
    public required string VirtualHost { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }

    public int MaxRetryCount { get; set; }
    public int RetryDelayMilliseconds { get; set; }
}
