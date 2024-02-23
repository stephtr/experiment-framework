namespace ExperimentFramework;

public abstract class RotationAxisComponent
{
    public abstract double TargetPosition { get; set; }
    public abstract double ActualPosition { get; }
}

[DisplayName("Rotation")]
[IconString("\xE7AD")]
public abstract class RotationComponent : ExperimentComponentClass
{
    public abstract RotationAxisComponent[] Axes { get; }
}

public class FakeRotationAxis : RotationAxisComponent
{
    public override double TargetPosition { get; set; }
    public override double ActualPosition
    {
        get
        {
            var noise = 1 + Math.Sin(DateTime.Now.Ticks);
            return TargetPosition + noise * 0.1;
        }
    }
}

[DisplayName("Debug")]
public class FakeRotation : RotationComponent
{
    public override RotationAxisComponent[] Axes { get; } = new FakeRotationAxis[] { new(), new(), new(), new() };
}
