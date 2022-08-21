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
    public abstract int MaxNumberPointsToMoveAlong(int axisCount);
    public abstract Task MoveAlongPath(IReadOnlyList<double[]> path, int[] axes, double frequency);
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

    public override int MaxNumberPointsToMoveAlong(int axisCount) => int.MaxValue;

    public override async Task MoveAlongPath(IReadOnlyList<double[]> path, int[] axes, double frequency)
    {
        if (axes.Length < 1 || axes.Length != path.Count || !path.All(p => p.Length == path[0].Length))
        {
            throw new ArgumentException("All path axes must have the same length");
        }

        var startDate = DateTime.UtcNow;
        var pointCount = path[0].Length;
        while (true)
        {
            var t = (DateTime.UtcNow - startDate).TotalSeconds;
            var pathIndex = Math.Min((int)Math.Floor(t * frequency), pointCount - 1);
            for (var iAx = 0; iAx < axes.Length; iAx++)
            {
                Axes[axes[iAx]].TargetPosition = path[iAx][pathIndex];
            }
            if (pathIndex == pointCount - 1)
            {
                break;
            }
            await Task.Delay(10);
        }
    }
}
