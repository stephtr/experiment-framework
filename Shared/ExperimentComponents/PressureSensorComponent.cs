namespace ExperimentFramework;

[DisplayName("Pressure Sensor")]
[IconString("\xE957")]
public abstract class PressureSensorComponent : ExperimentComponentClass
{
    public abstract float CurrentPressure { get; }
}

[DisplayName("Debug")]
public class FakePressureSensor : PressureSensorComponent
{
    private float currentPressure;
    public override float CurrentPressure => currentPressure;

    private bool isRunning = true;
    public FakePressureSensor() {
        async Task updateLoop()
        {
            var random = new Random();
            while (isRunning)
            {
                await Task.Delay(500);
                currentPressure = random.NextSingle();
            }
        }
        _ = updateLoop();
    }

    public override void Dispose()
    {
        isRunning = false;
    }
}
