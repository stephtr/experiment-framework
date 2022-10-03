using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ExperimentFramework;
using CommunityToolkit.Mvvm.Input;

namespace ExperimentFramework;

public partial class LaserViewModel : ObservableObject, IDisposable
{
    public bool IsAvailable { get => Laser != null; }

    public bool IsOn
    {
        get => Laser?.IsOn ?? false;
        set
        {
            if (Laser != null)
            {
                Laser.IsOn = value;
                OnPropertyChanged(nameof(IsOn));
                OnPropertyChanged(nameof(ActualPower));
                TriggerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAvailable))]
    [NotifyPropertyChangedFor(nameof(HasPowerControl))]
    [NotifyPropertyChangedFor(nameof(HasBurstControl))]
    [NotifyPropertyChangedFor(nameof(IsOn))]
    [NotifyPropertyChangedFor(nameof(MaxTargetPower))]
    [NotifyPropertyChangedFor(nameof(ActualPower))]
    [NotifyPropertyChangedFor(nameof(TargetPower))]
    [NotifyPropertyChangedFor(nameof(Mode))]
    [NotifyPropertyChangedFor(nameof(IsInBurstMode))]
    [NotifyPropertyChangedFor(nameof(BurstSize))]
    [NotifyPropertyChangedFor(nameof(BurstFrequencyDivider))]
    [NotifyCanExecuteChangedFor(nameof(TriggerCommand))]
    private LaserComponent? laser;

    public bool HasPowerControl => laser?.HasPowerControl ?? false;
    public bool HasBurstControl => laser?.HasBurstControl ?? false;

    public double MaxTargetPower
    {
        get
        {
            if (Laser == null || !Laser.HasPowerControl) return 0;
            return Laser.MaxTargetPower;
        }
    }
    public string ActualPower
    {
        get
        {
            if (Laser == null || !Laser.HasPowerControl) return "-";
            return Laser.ActualPower.ToString("N2") + " mW";
        }
    }
    public double TargetPower
    {
        get
        {
            if (Laser == null || !Laser.HasPowerControl) return 0;
            return Laser.TargetPower;
        }
        set
        {
            if (Laser != null && Laser.HasPowerControl)
            {
                Laser.TargetPower = Math.Clamp(value, 0, MaxTargetPower);
                OnPropertyChanged(nameof(TargetPower));
                OnPropertyChanged(nameof(ActualPower));
            }
        }
    }

    public bool CanTrigger => Laser != null && Laser.IsOn && Laser.HasBurstControl;
    [RelayCommand(CanExecute = nameof(CanTrigger))]
    private void Trigger()
    {
        Laser!.Burst();
    }

    public LaserMode[] Modes => (LaserMode[])Enum.GetValues(typeof(LaserMode));
    public LaserMode? Mode
    {
        get => Laser?.Mode;
        set
        {
            if (Laser != null && value != null)
            {
                Laser.Mode = value.Value;
                OnPropertyChanged(nameof(IsInBurstMode));
            }
        }
    }
    public bool IsInBurstMode => Laser?.Mode == LaserMode.Burst;

    public int BurstSize
    {
        get
        {
            if (Laser == null || !Laser.HasBurstControl) return 0;
            return Laser.BurstSize;
        }
        set
        {
            if (Laser != null)
            {
                Laser.BurstSize = value;
                OnPropertyChanged(nameof(BurstSize));
            }
        }
    }
    public int BurstFrequencyDivider
    {
        get
        {
            if (Laser == null || !Laser.HasBurstControl) return 0;
            return Laser.BurstFrequencyDivider;
        }
        set
        {
            if (Laser != null)
            {
                Laser.BurstFrequencyDivider = value;
                OnPropertyChanged(nameof(BurstFrequencyDivider));
            }
        }
    }

    private bool LoopRunning = true;
    public LaserViewModel()
    {
        Laser = ExperimentContainer.Singleton.GetActiveComponent<LaserComponent>();
        ExperimentContainer.Singleton.AddComponentChangeHandler<LaserComponent>((laser) => Laser = laser);

        var updateLoop = async () =>
        {
            while (LoopRunning)
            {
                OnPropertyChanged(nameof(IsOn));
                OnPropertyChanged(nameof(ActualPower));
                await Task.Delay(500);
            }
        };
        var controllerLoop = async () =>
        {
            var previousReading = GameController.GetReading();
            var lastTimeStamp = DateTime.UtcNow;
            while (LoopRunning)
            {
                var reading = GameController.GetReading();
                var dt = Math.Min(0.1, (DateTime.UtcNow - lastTimeStamp).TotalSeconds);
                lastTimeStamp = DateTime.UtcNow;

                if (reading.A && !previousReading.A)
                {
                    if (CanTrigger)
                    {
                        TriggerCommand.Execute(null);
                    }
                }

                previousReading = reading;
                await Task.Delay(20);
            }
        };
        updateLoop();
        controllerLoop();
    }

    public void Dispose()
    {
        LoopRunning = false;
    }
}

public sealed partial class LaserControl : UserControl
{
    public LaserViewModel? ViewModel { get; set; }

    public LaserControl()
    {
        this.InitializeComponent();
    }
}
