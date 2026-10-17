namespace LinePulse.Processing.Exceptions;

public enum KnownEventType
{
    CycleStarted,
    CycleCompleted,
    MachineStateChanged
}

public static class EventTypeExceptionsHandler
{
    public static void EnsureKnown(string eventType)
    {
        if (!Enum.TryParse<KnownEventType>(eventType, out _))
        {
            throw new InvalidOperationException($"Unrecognized event type '{eventType}'.");
        }
    }
}
