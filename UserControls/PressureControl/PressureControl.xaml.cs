﻿using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ExperimentFramework;

public partial class PressureViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PressureText))]
    private PressureSensorComponent? sensor;

    private static string FormatPressure(float pressure)
    {
        return pressure.ToString(
            pressure >= 1 ?
                (pressure >= 10 ? "F1" : "F2") :
                "0.00 e+0"
        );
    }

    public string PressureText => Sensor?.SensorStatus switch
    {
        null => "",
        PressureSensorStatus.Ok => $"{FormatPressure(Sensor.CurrentPressure)} mbar",
        PressureSensorStatus.Unknown => "",
        PressureSensorStatus.Error => "error",
        PressureSensorStatus.Overrange => "overrange",
        PressureSensorStatus.Underrange => "underrange",
        _ => throw new NotImplementedException(),
    };

    private bool LoopRunning = true;
    public PressureViewModel()
    {
        Sensor = ExperimentContainer.Singleton.GetActiveComponent<PressureSensorComponent>();
        ExperimentContainer.Singleton.AddComponentChangeHandler<PressureSensorComponent>((sensor) => Sensor = sensor);

        async Task updateLoop()
        {
            while (LoopRunning)
            {
                OnPropertyChanged(nameof(PressureText));
                await Task.Delay(500);
            }
        }
        _ = updateLoop();
    }

    public void Dispose()
    {
        LoopRunning = false;
    }
}

public sealed partial class PressureControl : UserControl
{
    public PressureViewModel? ViewModel { get; set; }

    public PressureControl()
    {
        this.InitializeComponent();
    }
}
