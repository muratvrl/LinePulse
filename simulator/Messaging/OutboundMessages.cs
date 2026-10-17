namespace LinePulse.Simulator.Messaging;

public enum SimulatedMachineState
{
    Running,
    Waiting
}

public record CycleStartedMessage(string EventType, Guid StationId, Guid CorrelationId, DateTimeOffset OccurredAt);

public record CycleCompletedMessage(string EventType, Guid StationId, Guid CorrelationId, DateTimeOffset OccurredAt);

public record MachineStateChangedMessage(
    string EventType,
    Guid StationId,
    Guid MachineId,
    SimulatedMachineState PreviousState,
    SimulatedMachineState CurrentState,
    Guid? CorrelationId,
    DateTimeOffset OccurredAt);
