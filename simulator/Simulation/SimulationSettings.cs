namespace LinePulse.Simulator.Simulation;

public class SimulationSettings
{
    public const string SectionName = "Simulation";

    public double DelayScaleFactor { get; set; }
    public double FaultInjectionRate { get; set; }
    public Guid FaultInjectionFakeId { get; set; }

    public List<StationProfile> Stations { get; set; } = [];

    public List<MachineProfile> Machines { get; set; } = [];

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (DelayScaleFactor <= 0)
            errors.Add("DelayScaleFactor must be greater than 0.");
        if (FaultInjectionRate is < 0 or > 1)
            errors.Add("FaultInjectionRate must be between 0 and 1.");

        var stationIds = new HashSet<Guid>();
        foreach (var station in Stations)
        {
            if (!stationIds.Add(station.StationId))
                errors.Add($"Duplicate StationId: {station.StationId}.");

            errors.AddRange(station.Validate());
        }

        foreach (var machine in Machines)
        {
            if (!stationIds.Contains(machine.StationId))
                errors.Add($"Machine {machine.MachineId} references unknown StationId {machine.StationId}.");

            errors.AddRange(machine.Validate());
        }

        return errors;
    }
}

public class StationProfile
{
    public Guid StationId { get; set; }
    public long ExpectedCycleMilliseconds { get; set; }
    public long ToleranceMilliseconds { get; set; }

    public double OverrunWeight { get; set; }
    public double UnderrunWeight { get; set; }

    public long IdleBetweenCyclesMilliseconds { get; set; }

    public long NormalFloorMilliseconds { get; set; }

    public double OverrunRangeMultiplier { get; set; }
    public double UnderrunRangeMultiplier { get; set; }

    public IReadOnlyList<WeightedRange> GetCycleDurationBuckets()
    {
        var allowedMinimum = ExpectedCycleMilliseconds - ToleranceMilliseconds;
        var allowedMaximum = ExpectedCycleMilliseconds + ToleranceMilliseconds;
        var normalMin = Math.Max(NormalFloorMilliseconds, allowedMinimum);
        var overrunMax = allowedMaximum + (long)(OverrunRangeMultiplier * ToleranceMilliseconds);
        var underrunMin = Math.Max(NormalFloorMilliseconds, normalMin - (long)(UnderrunRangeMultiplier * ToleranceMilliseconds));

        return
        [
            new WeightedRange(UnderrunWeight, underrunMin, Math.Max(underrunMin, normalMin - 1)),
            new WeightedRange(1.0 - OverrunWeight - UnderrunWeight, normalMin, allowedMaximum),
            new WeightedRange(OverrunWeight, allowedMaximum + 1, Math.Max(allowedMaximum + 1, overrunMax))
        ];
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        var prefix = $"Station {StationId}: ";

        if (ExpectedCycleMilliseconds <= 0)
            errors.Add(prefix + "ExpectedCycleMilliseconds must be greater than 0.");
        if (ToleranceMilliseconds < 0)
            errors.Add(prefix + "ToleranceMilliseconds cannot be negative.");
        if (OverrunWeight is < 0 or > 1)
            errors.Add(prefix + "OverrunWeight must be between 0 and 1.");
        if (UnderrunWeight is < 0 or > 1)
            errors.Add(prefix + "UnderrunWeight must be between 0 and 1.");
        if (OverrunWeight + UnderrunWeight > 1)
            errors.Add(prefix + "OverrunWeight + UnderrunWeight cannot exceed 1.");
        if (IdleBetweenCyclesMilliseconds < 0)
            errors.Add(prefix + "IdleBetweenCyclesMilliseconds cannot be negative.");
        if (NormalFloorMilliseconds < 0)
            errors.Add(prefix + "NormalFloorMilliseconds cannot be negative.");
        if (NormalFloorMilliseconds > ExpectedCycleMilliseconds + ToleranceMilliseconds)
            errors.Add(prefix + "NormalFloorMilliseconds cannot exceed ExpectedCycleMilliseconds + ToleranceMilliseconds.");
        if (OverrunRangeMultiplier < 0)
            errors.Add(prefix + "OverrunRangeMultiplier cannot be negative.");
        if (UnderrunRangeMultiplier < 0)
            errors.Add(prefix + "UnderrunRangeMultiplier cannot be negative.");

        return errors;
    }
}

public class MachineProfile
{
    public Guid MachineId { get; set; }
    public Guid StationId { get; set; }
    public long MinDowntimeMilliseconds { get; set; }
    public long MicroStopLimitMilliseconds { get; set; }

    public double NoiseWeight { get; set; }

    public double MicroStopWeight { get; set; }

    public double MajorStopWeight { get; set; }

    public long RunningPeriodMilliseconds { get; set; }

    public long NoiseFloorMilliseconds { get; set; }

    public double MajorStopRangeMultiplier { get; set; }

    public IReadOnlyList<WeightedRange> GetWaitingDurationBuckets()
    {
        var noiseMax = Math.Max(NoiseFloorMilliseconds, MinDowntimeMilliseconds - 1);
        var majorStopMax = Math.Max(MicroStopLimitMilliseconds, (long)(MicroStopLimitMilliseconds * MajorStopRangeMultiplier));

        return
        [
            new WeightedRange(NoiseWeight, NoiseFloorMilliseconds, noiseMax),
            new WeightedRange(MicroStopWeight, MinDowntimeMilliseconds, MicroStopLimitMilliseconds - 1),
            new WeightedRange(MajorStopWeight, MicroStopLimitMilliseconds, majorStopMax)
        ];
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        var prefix = $"Machine {MachineId}: ";

        if (MinDowntimeMilliseconds <= 0)
            errors.Add(prefix + "MinDowntimeMilliseconds must be greater than 0.");
        if (MicroStopLimitMilliseconds <= MinDowntimeMilliseconds)
            errors.Add(prefix + "MicroStopLimitMilliseconds must be greater than MinDowntimeMilliseconds.");
        if (NoiseWeight < 0)
            errors.Add(prefix + "NoiseWeight cannot be negative.");
        if (MicroStopWeight < 0)
            errors.Add(prefix + "MicroStopWeight cannot be negative.");
        if (MajorStopWeight < 0)
            errors.Add(prefix + "MajorStopWeight cannot be negative.");
        if (RunningPeriodMilliseconds < 0)
            errors.Add(prefix + "RunningPeriodMilliseconds cannot be negative.");
        if (NoiseFloorMilliseconds < 0)
            errors.Add(prefix + "NoiseFloorMilliseconds cannot be negative.");
        if (MajorStopRangeMultiplier < 0)
            errors.Add(prefix + "MajorStopRangeMultiplier cannot be negative.");

        return errors;
    }
}
