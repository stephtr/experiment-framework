namespace ExperimentFramework;

public enum OscilloscopeTriggerMode
{
    Auto,
    Normal,
    Single,
    Stop,
}

[DisplayName("Oscilloscope")]
[IconString("\xE9D9")]
public abstract class OscilloscopeComponent : ExperimentComponentClass
{
    public abstract OscilloscopeTriggerMode TriggerMode { get; set; }
    public abstract Task<(float, float)[]> GetWaveform(string channel);
    public abstract Task WaitForNewData(CancellationToken cancelToken = default);
}

public class FakeOscilloscopeSettings
{
}

[DisplayName("Debug")]
public class FakeOscilloscope : OscilloscopeComponent
{
    public FakeOscilloscope(FakeOscilloscopeSettings settings) { }

    public override OscilloscopeTriggerMode TriggerMode { get; set; } = OscilloscopeTriggerMode.Stop;

    public override Task<(float, float)[]> GetWaveform(string channel)
    {
        var n = 1000;
        var res = new (float, float)[n];
        for (int i = 0; i < n; i++)
        {
            res[i] = (i / (float)n, (float)Math.Sin(i * 2 * Math.PI / n));
        }
        return Task.FromResult(res);
    }

    public override Task WaitForNewData(CancellationToken cancelToken = default)
    {
        return Task.Delay(1000, cancelToken);
    }
}
