namespace ExperimentFramework;

[DisplayName("Laser")]
[IconString("\xE754")]
public abstract class LaserComponent : ExperimentComponentClass
{
    public abstract bool IsOn { get; set; }
    public abstract double TargetPower { get; set; }
    public abstract double MaxTargetPower { get; }
    public abstract double ActualPower { get; }
}

public class FakeLaserSettings
{
    [Options(nameof(TestValues))]
    public string Test { get; set; } = "";
    public static IEnumerable<string> TestValues { get => new string[] { "a", "b", "c" }; }
}

[DisplayName("Debug")]
public class FakeLaser : LaserComponent
{
    public FakeLaser(FakeLaserSettings settings) { }
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
}
