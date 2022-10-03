namespace ExperimentFramework;

public enum LaserMode {
    Continuous,
    Burst,
}

[DisplayName("Laser")]
[IconString("\xE754")]
public abstract class LaserComponent : ExperimentComponentClass
{
    public abstract bool HasPowerControl { get; }
    public abstract bool HasBurstControl { get; }
    public abstract bool IsOn { get; set; }
    public abstract double TargetPower { get; set; }
    public abstract double MaxTargetPower { get; }
    public abstract double ActualPower { get; }
    public abstract LaserMode Mode { get; set; }
    public abstract int BurstSize { get; set; }
    public abstract int BurstFrequencyDivider { get; set; }
    public abstract void Burst();
}

public class FakeLaserSettings
{
    [Options(nameof(TestValues))]
    public string Test { get; set; } = "";
    public static IEnumerable<string> TestValues { get => new string[] { "a", "b", "c" }; }
}

[DisplayName("Debug (cw)")]
public class FakeLaser : LaserComponent
{
    public override bool HasPowerControl => true;
    public override bool HasBurstControl => false;

    public FakeLaser(FakeLaserSettings settings)
    {
        Thread.Sleep(2000);
    }
    public override bool IsOn { get; set; }
    private double power = 0;
    public override double TargetPower { get => power; set => power = Math.Clamp(value, 0, MaxTargetPower); }
    public override double MaxTargetPower { get => 1000; }
    public override double ActualPower
    {
        get
        {
            if (!IsOn) return 0;
            var noise = 1 + Math.Sin(DateTime.Now.Ticks);
            return power + noise * 10;
        }
    }
    public override LaserMode Mode { get => LaserMode.Continuous; set { if (value != LaserMode.Continuous) throw new NotSupportedException(); } }
    public override int BurstSize { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int BurstFrequencyDivider { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Burst() { throw new NotSupportedException(); }
}

[DisplayName("Debug (pulsed)")]
public class FakePulsedLaser : LaserComponent
{
    public override bool HasPowerControl => false;
    public override bool HasBurstControl => true;

    public FakePulsedLaser(FakeLaserSettings settings)
    {
        Thread.Sleep(2000);
    }
    public override bool IsOn { get; set; }
    public override double TargetPower { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override double MaxTargetPower { get => throw new NotSupportedException(); }
    public override double ActualPower { get => throw new NotSupportedException(); }
    public override LaserMode Mode { get; set; } = LaserMode.Burst;
    public override int BurstSize { get; set; } = 1;
    public override int BurstFrequencyDivider { get; set; } = 1;
    public override void Burst() { }
}
