using System.Text.Json;
using System.Text.Json.Serialization;
using LinePulse.Messaging;
using LinePulse.Processing;
using LinePulse.Processing.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LinePulse.RabbitMq;

public class EventConsumerHostedService(
    IConnectionFactory connectionFactory,
    IEventHandler<CycleStartedMessage> cycleStartedHandler,
    IEventHandler<CycleCompletedMessage> cycleCompletedHandler,
    IEventHandler<MachineStateChangedMessage> machineStateChangedHandler,
    IOptions<Settings> options,
    ILogger<EventConsumerHostedService> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var connection =
            await connectionFactory.CreateConnectionAsync(stoppingToken);

        await using var channel =
            await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareTopologyAsync(channel, options.Value, stoppingToken);

        var cycleConsumer = new AsyncEventingBasicConsumer(channel);
        cycleConsumer.ReceivedAsync += (_, eventArgs) =>
            ProcessMessageAsync(channel, eventArgs, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: RabbitMqEventRouter.CycleProcessingQueue,
            autoAck: false,
            consumer: cycleConsumer,
            cancellationToken: stoppingToken);

        var machineConsumer = new AsyncEventingBasicConsumer(channel);
        machineConsumer.ReceivedAsync += (_, eventArgs) =>
            ProcessMessageAsync(channel, eventArgs, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: RabbitMqEventRouter.MachineProcessingQueue,
            autoAck: false,
            consumer: machineConsumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Connected to RabbitMQ listening on {CycleQueue} and {MachineQueue}.",
            RabbitMqEventRouter.CycleProcessingQueue, RabbitMqEventRouter.MachineProcessingQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (eventArgs.RoutingKey)
            {
                case RabbitMqEventRouter.CycleStartedRoutingKey:
                    var cycleStarted = Deserialize<CycleStartedMessage>(eventArgs);
                    await cycleStartedHandler.HandleAsync(
                        cycleStarted,
                        cancellationToken);
                    break;

                case RabbitMqEventRouter.CycleCompletedRoutingKey:
                    var cycleCompleted = Deserialize<CycleCompletedMessage>(eventArgs);
                    await cycleCompletedHandler.HandleAsync(
                        cycleCompleted,
                        cancellationToken);
                    break;

                case RabbitMqEventRouter.MachineStateRoutingKey:
                    var machineStateChanged =
                        Deserialize<MachineStateChangedMessage>(eventArgs);

                    await machineStateChangedHandler.HandleAsync(
                        machineStateChanged,
                        cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unrecognized routing key '{eventArgs.RoutingKey}'.");
            }

            await channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            var retryCount = GetRetryCount(eventArgs);

            if (TransientErrorClassifier.IsTransient(ex, cancellationToken) && retryCount < options.Value.MaxRetryCount)
            {
                var (published, _) = await RetryQueuePublisher.TryPublishAsync(
                    channel, eventArgs, retryCount + 1, cancellationToken);

                if (published)
                {
                    await channel.BasicAckAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await channel.BasicNackAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: true,
                        cancellationToken: cancellationToken);
                }

                return;
            }

            var (deadLettered, _) = await DeadLetterPublisher.TryPublishAsync(
                channel, eventArgs, ex, cancellationToken);

            if (deadLettered)
            {
                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private static int GetRetryCount(BasicDeliverEventArgs eventArgs)
    {
        if (eventArgs.BasicProperties.Headers is not { } headers
            || !headers.TryGetValue(RabbitMqEventRouter.RetryCountHeader, out var value)
            || value is null)
        {
            return 0;
        }

        return Convert.ToInt32(value);
    }

    private static TMessage Deserialize<TMessage>(
        BasicDeliverEventArgs eventArgs)
        where TMessage : IEventEnvelope
    {
        var message = JsonSerializer.Deserialize<TMessage>(
                   eventArgs.Body.Span,
                   JsonOptions)
               ?? throw new InvalidOperationException(
                   "Deserialized message is null.");
        EventTypeExceptionsHandler.EnsureKnown(message.EventType);

        return message;
    }

    private static async Task DeclareTopologyAsync(IChannel channel, Settings settings, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(RabbitMqEventRouter.CycleDeadLetterExchange, ExchangeType.Fanout,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(RabbitMqEventRouter.CycleDeadLetterQueue,
            durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqEventRouter.CycleDeadLetterQueue, RabbitMqEventRouter.CycleDeadLetterExchange,
            routingKey: string.Empty, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(RabbitMqEventRouter.MachineDeadLetterExchange, ExchangeType.Fanout,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(RabbitMqEventRouter.MachineDeadLetterQueue,
            durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqEventRouter.MachineDeadLetterQueue, RabbitMqEventRouter.MachineDeadLetterExchange,
            routingKey: string.Empty, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(RabbitMqEventRouter.EventsExchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);

        var cycleQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = RabbitMqEventRouter.CycleDeadLetterExchange
        };

        await channel.QueueDeclareAsync(RabbitMqEventRouter.CycleProcessingQueue,
            durable: true, exclusive: false, autoDelete: false,
            arguments: cycleQueueArguments, cancellationToken: cancellationToken);

        await channel.QueueBindAsync(RabbitMqEventRouter.CycleProcessingQueue, RabbitMqEventRouter.EventsExchange,
            routingKey: RabbitMqEventRouter.CycleProcessingQueueBindingPattern, cancellationToken: cancellationToken);

        var machineQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = RabbitMqEventRouter.MachineDeadLetterExchange
        };

        await channel.QueueDeclareAsync(RabbitMqEventRouter.MachineProcessingQueue,
            durable: true, exclusive: false, autoDelete: false,
            arguments: machineQueueArguments, cancellationToken: cancellationToken);

        await channel.QueueBindAsync(RabbitMqEventRouter.MachineProcessingQueue, RabbitMqEventRouter.EventsExchange,
            routingKey: RabbitMqEventRouter.MachineProcessingQueueBindingPattern, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(RabbitMqEventRouter.RetryExchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);

        var retryQueueArguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = settings.RetryDelayMilliseconds,
            ["x-dead-letter-exchange"] = RabbitMqEventRouter.EventsExchange
        };

        await channel.QueueDeclareAsync(RabbitMqEventRouter.CycleRetryQueue,
            durable: true, exclusive: false, autoDelete: false,
            arguments: retryQueueArguments, cancellationToken: cancellationToken);

        await channel.QueueBindAsync(RabbitMqEventRouter.CycleRetryQueue, RabbitMqEventRouter.RetryExchange,
            routingKey: RabbitMqEventRouter.CycleProcessingQueueBindingPattern, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(RabbitMqEventRouter.MachineRetryQueue,
            durable: true, exclusive: false, autoDelete: false,
            arguments: retryQueueArguments, cancellationToken: cancellationToken);

        await channel.QueueBindAsync(RabbitMqEventRouter.MachineRetryQueue, RabbitMqEventRouter.RetryExchange,
            routingKey: RabbitMqEventRouter.MachineProcessingQueueBindingPattern, cancellationToken: cancellationToken);
    }
}
