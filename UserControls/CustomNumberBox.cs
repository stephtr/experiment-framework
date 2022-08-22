using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Globalization.NumberFormatting;
using Windows.System;

namespace ExperimentFramework;

public class CustomNumberBox : NumberBox
{
    public CustomNumberBox() : base()
    {
        NumberFormatter = new DecimalFormatter { SignificantDigits = 3, NumberRounder = new SignificantDigitsNumberRounder { SignificantDigits = 3 } };
        ValueChanged += CustomNumberBox_ValueChanged;
    }

    private void UpdateChange(bool fastMode = false)
    {
        if (Value == 0)
        {
            LargeChange = 1;
            SmallChange = 0.1;
            return;
        }
        var value = Math.Abs(Value);
        var logVal = Math.Log10(value);
        var expFirstSignificantDigit = (int)(logVal >= 0 ? logVal * 1.0000001 : Math.Floor(logVal / 1.0000001));
        LargeChange = Math.Pow(10, expFirstSignificantDigit);
        SmallChange = Math.Pow(10, expFirstSignificantDigit - (fastMode ? 1 : 2));
    }

    private void CustomNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        UpdateChange();
    }

    private Queue<DateTime> LastChanges = new Queue<DateTime>();
    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down || e.Key == VirtualKey.PageUp || e.Key == VirtualKey.PageDown)
        {
            var fastMode = false;
            if (LastChanges.Count > 5)
            {
                fastMode = DateTime.UtcNow - LastChanges.Dequeue() < TimeSpan.FromMilliseconds(800);
            }
            LastChanges.Enqueue(DateTime.UtcNow);

            UpdateChange(fastMode);
        }
        base.OnPreviewKeyDown(e);
    }
}

public class PositionNumberBox : NumberBox
{
    public PositionNumberBox() : base()
    {
        NumberFormatter = new DecimalFormatter()
        {
            FractionDigits = 3,
            IsGrouped = true,
            NumberRounder = new IncrementNumberRounder { Increment = 0.001 },
        };
    }

    private Queue<DateTime> LastChanges = new Queue<DateTime>();
    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down || e.Key == VirtualKey.PageUp || e.Key == VirtualKey.PageDown)
        {
            var fastMode = false;
            if (LastChanges.Count > 5)
            {
                fastMode = DateTime.UtcNow - LastChanges.Dequeue() < TimeSpan.FromMilliseconds(800);
            }
            LastChanges.Enqueue(DateTime.UtcNow);

            SmallChange = fastMode ? 0.05 : 0.01;
            LargeChange = fastMode ? 5 : 1;
        }
        base.OnPreviewKeyDown(e);
    }
}
