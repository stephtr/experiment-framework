namespace ExperimentFramework;

[DisplayName("Oscilloscope")]
[IconString("\xE9D9")]
public abstract class OscilloscopeComponent : ExperimentComponentClass
{
    public abstract float[] GetWaveform(string channel);
}

public class FakeOscilloscopeSettings
{
}

[DisplayName("Debug")]
public class FakeOscilloscope : OscilloscopeComponent
{
    public FakeOscilloscope(FakeOscilloscopeSettings settings) { }

    public override float[] GetWaveform(string channel)
    {
        var n = 1000;
        var res = new float[n];
        for (int i = 0; i < n; i++)
        {
            res[i] = (float)Math.Sin(i * 2 * Math.PI / n);
        }
        return res;
    }
}
