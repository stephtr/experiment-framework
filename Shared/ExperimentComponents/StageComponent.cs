namespace ExperimentFramework;

public abstract class AxisComponent
{
    public abstract double TargetPosition { get; set; }
    public abstract double ActualPosition { get; }
    public abstract double MinPosition { get; }
    public abstract double MaxPosition { get; }
}

[DisplayName("Stage")]
[IconString("\xE759")]
public abstract class StageComponent : ExperimentComponentClass
{
    public abstract AxisComponent[] Axes { get; }
    public abstract Task ScanCircle(double radius, double period, int circles = 1);
}

public class FakeAxis : AxisComponent
{
    private double position = 0;
    public override double TargetPosition { get => position; set => position = Math.Clamp(value, MinPosition, MaxPosition); }
    public override double ActualPosition
    {
        get
        {
            var noise = 1 + Math.Sin(DateTime.Now.Ticks);
            return position + noise * 0.1;
        }
    }
    public override double MinPosition { get => 0; }
    public override double MaxPosition { get => 1000; }
}

[DisplayName("Debug")]
public class FakeStage : StageComponent
{
    public override AxisComponent[] Axes { get; } = new FakeAxis[] { new(), new(), new() };
    public override async Task ScanCircle(double radius, double period, int circles = 1)
    {
        var startDate = DateTime.UtcNow;
        var targetX = Axes[0].TargetPosition;
        var targetY = Axes[0].TargetPosition;
        while (true)
        {
            var now = DateTime.UtcNow;
            var round = (now - startDate).TotalSeconds / period;
            if (round > circles) break;
            var phi = 2 * Math.PI * round;
            Axes[0].TargetPosition = targetX + radius * Math.Sin(phi);
            Axes[1].TargetPosition = targetY + radius * Math.Cos(phi);
            await Task.Delay(10);
        }
        Axes[0].TargetPosition = targetX;
        Axes[1].TargetPosition = targetY;
    }
}
