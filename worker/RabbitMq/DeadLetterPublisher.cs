using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LinePulse.RabbitMq;

public static class DeadLetterPublisher
{
    public static async Task<(bool Success, Exception? Error)> TryPublishAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        Exception reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var headers = eventArgs.BasicProperties.Headers is { } existingHeaders
                ? new Dictionary<string, object?>(existingHeaders)
                : new Dictionary<string, object?>();

            headers[RabbitMqEventRouter.FailureReasonHeader] = $"{reason.GetType().Name}: {reason.Message}";

            var deadLetterProperties = new BasicProperties(eventArgs.BasicProperties)
            {
                Headers = headers
            };

            await channel.BasicPublishAsync(
                exchange: GetDeadLetterExchange(eventArgs.RoutingKey),
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: deadLetterProperties,
                body: eventArgs.Body,
                cancellationToken: cancellationToken);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    private static string GetDeadLetterExchange(string routingKey) =>
        routingKey switch
        {
            RabbitMqEventRouter.CycleStartedRoutingKey or RabbitMqEventRouter.CycleCompletedRoutingKey
                => RabbitMqEventRouter.CycleDeadLetterExchange,
            RabbitMqEventRouter.MachineStateRoutingKey
                => RabbitMqEventRouter.MachineDeadLetterExchange,
            _ => throw new InvalidOperationException($"Unrecognized routing key '{routingKey}'.")
        };
}
