using RabbitMQ.Client;

namespace LinePulse.Simulator.Messaging;

public static class RabbitMqEventRouter
{
    public const string EventsExchange = "production.events";

    public const string CycleStartedRoutingKey = "production.cycle.started";
    public const string CycleCompletedRoutingKey = "production.cycle.completed";
    public const string MachineStateRoutingKey = "production.machine.state";

    public const string CycleStartedEventType = "CycleStarted";
    public const string CycleCompletedEventType = "CycleCompleted";
    public const string MachineStateChangedEventType = "MachineStateChanged";

    public const string CycleProcessingQueue = "production.cycle.queue";
    public const string CycleProcessingQueueBindingPattern = "production.cycle.#";

    public const string MachineProcessingQueue = "production.machine.queue";
    public const string MachineProcessingQueueBindingPattern = "production.machine.#";

    public const string CycleDeadLetterExchange = "production.events.cycle.dlx";
    public const string CycleDeadLetterQueue = "production.cycle.dlq";

    public const string MachineDeadLetterExchange = "production.events.machine.dlx";
    public const string MachineDeadLetterQueue = "production.machine.dlq";

    public static async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(CycleDeadLetterExchange, ExchangeType.Fanout,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(CycleDeadLetterQueue,
            durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(CycleDeadLetterQueue, CycleDeadLetterExchange,
            routingKey: string.Empty, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(MachineDeadLetterExchange, ExchangeType.Fanout,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(MachineDeadLetterQueue,
            durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(MachineDeadLetterQueue, MachineDeadLetterExchange,
            routingKey: string.Empty, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);

        var cycleQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = CycleDeadLetterExchange
        };
        await channel.QueueDeclareAsync(CycleProcessingQueue,
            durable: true, exclusive: false, autoDelete: false,
            arguments: cycleQueueArguments, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(CycleProcessingQueue, EventsExchange,
            routingKey: CycleProcessingQueueBindingPattern, cancellationToken: cancellationToken);

        var machineQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = MachineDeadLetterExchange
        };
        await channel.QueueDeclareAsync(MachineProcessingQueue,
            durable: true, exclusive: false, autoDelete: false,
            arguments: machineQueueArguments, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(MachineProcessingQueue, EventsExchange,
            routingKey: MachineProcessingQueueBindingPattern, cancellationToken: cancellationToken);
    }
}
