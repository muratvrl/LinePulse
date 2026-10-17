namespace LinePulse.Simulator.Messaging;

public class RabbitMqSettings
{ 
    public  const string SectionName = "RabbitMq";
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string VirtualHost { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
