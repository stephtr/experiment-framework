namespace ExperimentFramework;

public enum AnalogChannelRange
{
    Range10V,
    Range1V,
    Range100mV,
    Range10mV,
}

public abstract class ChannelComponent
{
    public abstract AnalogChannelRange Range { get; set; }
    public abstract double Voltage { get; }
}

[DisplayName("ADC")]
[IconString("\xEC4A")]
public abstract class ADCComponent : ExperimentComponentClass
{
    public abstract ChannelComponent[] Channels { get; }
}

public class FakeChannel : ChannelComponent
{
    public override AnalogChannelRange Range { get => AnalogChannelRange.Range10V; set => throw new NotImplementedException(); }

    public override double Voltage => 2 + Math.Cos(DateTime.Now.Ticks);
}

[DisplayName("Debug")]
public class FakeADC : ADCComponent
{
    private const int numChannels = 14;
    public ChannelComponent[] channels = new ChannelComponent[numChannels];
    public override ChannelComponent[] Channels => channels.ToArray();

    public FakeADC()
    {
        for (var i = 0; i < numChannels; i++)
        {
            channels[i] = new FakeChannel();
        }
    }
}
