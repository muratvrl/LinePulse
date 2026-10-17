namespace LinePulse.RabbitMq;

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

    public const string RetryExchange = "production.events.retry";
    public const string CycleRetryQueue = "production.cycle.retry";
    public const string MachineRetryQueue = "production.machine.retry";

    public const string RetryCountHeader = "retry-count";
    public const string FailureReasonHeader = "x-failure-reason";
}
