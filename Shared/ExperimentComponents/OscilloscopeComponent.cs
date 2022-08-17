namespace ExperimentFramework;

[DisplayName("Oscilloscope")]
[IconString("\xE9D9")]
public abstract class OscilloscopeComponent : ExperimentComponentClass
{
}

public class FakeOscilloscopeSettings
{
}

[DisplayName("Debug")]
public class FakeOscilloscope : OscilloscopeComponent
{
    public FakeOscilloscope(FakeOscilloscopeSettings settings) { }
}
