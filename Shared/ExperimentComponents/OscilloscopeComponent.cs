namespace ExperimentFramework;

public enum OscilloscopeTriggerMode
{
    Auto,
    Normal,
    Single,
    Stop,
}

public struct OscilloscopeWaveform
{
    public (float Time, float Value)[] Data;
    public DateTime CreationTime;
    public (double, double)? DisplayBound;
    public (double, double) Timespan;
}

[DisplayName("Oscilloscope")]
[IconString("\xE9D9")]
public abstract class OscilloscopeComponent : ExperimentComponentClass
{
    public abstract OscilloscopeTriggerMode TriggerMode { get; set; }
    public abstract Task<OscilloscopeWaveform> GetWaveform(string channel);
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

    public override Task<OscilloscopeWaveform> GetWaveform(string channel)
    {
        var n = 1000;
        var data = new (float, float)[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = (i / (float)n, (float)Math.Sin(i * 2 * Math.PI / n));
        }
        return Task.FromResult(new OscilloscopeWaveform
        {
            Data = data,
            CreationTime = DateTime.Now,
            DisplayBound = (-1.5, 1.5),
            Timespan = (0, 1),
        });
    }

    public override Task WaitForNewData(CancellationToken cancelToken = default)
    {
        return Task.Delay(1000, cancelToken);
    }
}
