using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LinePulse.RabbitMq;

public static class RetryQueuePublisher
{
    public static async Task<(bool Success, Exception? Error)> TryPublishAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        int newRetryCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var headers = eventArgs.BasicProperties.Headers is { } existingHeaders
                ? new Dictionary<string, object?>(existingHeaders)
                : new Dictionary<string, object?>();

            headers[RabbitMqEventRouter.RetryCountHeader] = newRetryCount;

            var retryProperties = new BasicProperties(eventArgs.BasicProperties)
            {
                Headers = headers
            };

            await channel.BasicPublishAsync(
                exchange: RabbitMqEventRouter.RetryExchange,
                routingKey: eventArgs.RoutingKey,
                mandatory: false,
                basicProperties: retryProperties,
                body: eventArgs.Body,
                cancellationToken: cancellationToken);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }
}
