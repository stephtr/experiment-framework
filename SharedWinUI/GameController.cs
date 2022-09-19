using Windows.Gaming.Input;

namespace ExperimentFramework;

public class GameControllerReading
{
    public double Timestamp;
    public double[] Axis;
    public int[] AxisDiscrete;
    public bool A;
    public bool B;
    public bool X;
    public bool Y;

    public GameControllerReading()
    {
        Axis = new double[5] { 0, 0, 0, 0, 0 };
        AxisDiscrete = new int[3] { 0, 0, 0 };
    }
}

public class GameController
{
    private static double CleanAxisValue(double raw, double deadSpace = 0.2)
    {
        double transform(double abs) => Math.Exp(Math.Pow(abs, 4));

        var abs = Math.Abs(raw);
        if (abs < deadSpace)
        {
            abs = 0;
        }
        else
        {
            abs = (abs - deadSpace) / (1 - deadSpace);
        }

        return Math.Sign(raw) * Math.Clamp((transform(abs) - transform(0)) / (transform(1.0) - transform(0)), 0, 1);
    }

    private static DateTime ToDateTime(ulong raw)
    {
        var span = TimeSpan.FromMilliseconds(raw);
        return new DateTime(1970, 1, 1).Add(span);
    }

    public static GameControllerReading GetReading()
    {
        if (Gamepad.Gamepads.Count != 1)
        {
            return new GameControllerReading();
        }
        var reading = Gamepad.Gamepads[0].GetCurrentReading();
        return new GameControllerReading
        {
            Axis = new double[]{
                    CleanAxisValue(reading.LeftThumbstickX),
                    CleanAxisValue(reading.LeftThumbstickY),
                    CleanAxisValue(reading.RightTrigger - reading.LeftTrigger, 0),
                    CleanAxisValue(reading.RightThumbstickX),
                    CleanAxisValue(reading.RightThumbstickY),
                },
            AxisDiscrete = new int[]{
                    reading.Buttons.HasFlag(GamepadButtons.DPadLeft) ? -1 : reading.Buttons.HasFlag(GamepadButtons.DPadRight) ? +1 : 0,
                    reading.Buttons.HasFlag(GamepadButtons.DPadDown) ? -1 : reading.Buttons.HasFlag(GamepadButtons.DPadUp) ? +1 : 0,
                    reading.Buttons.HasFlag(GamepadButtons.LeftShoulder) ? -1 : reading.Buttons.HasFlag(GamepadButtons.RightShoulder) ? +1 : 0,
                },
            A = reading.Buttons.HasFlag(GamepadButtons.A),
            B = reading.Buttons.HasFlag(GamepadButtons.B),
            X = reading.Buttons.HasFlag(GamepadButtons.X),
            Y = reading.Buttons.HasFlag(GamepadButtons.Y),
            Timestamp = reading.Timestamp / 1_000_000f,
        };

    }
}
