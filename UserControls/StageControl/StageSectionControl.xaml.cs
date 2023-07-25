using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace ExperimentFramework;

internal partial class AxisViewModel : ObservableObject
{
    public AxisComponent Axis { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    private readonly bool SavePosition;
    public bool AxisInverted { get; set; }
    public double MinPosition { get => Axis.MinPosition; }
    public double MaxPosition { get => Axis.MaxPosition; }
    public string ActualPositionFormatted { get => Axis.ActualPosition.ToString("N3"); }
    public double TargetPosition
    {
        get => Axis.TargetPosition;
        set
        {
            if (IsLocked) return;
            Axis.TargetPosition = Math.Clamp(value, MinPosition, MaxPosition);
            if (SavePosition)
            {
                DebouncedSave(Id, Axis.TargetPosition);
            }
            OnPropertyChanged(nameof(TargetPosition));
            OnPropertyChanged(nameof(ActualPositionFormatted));
        }
    }
    private Action<string, double> DebouncedSave = TaskUtils.Debounce((string id, double position) =>
    {
        ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{id}"] = position;
    });

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnlocked))]
    private bool isLocked = false;
    public bool IsUnlocked => !IsLocked;

    [ObservableProperty]
    private float stepSize = 1f;

    public void UpdatePosition()
    {
        OnPropertyChanged(nameof(ActualPositionFormatted));
    }

    public AxisViewModel(AxisComponent axis, string name, string id, bool axisInverted, bool savePosition)
    {
        Axis = axis;
        Id = id;
        Name = name + ":";
        AxisInverted = axisInverted;
        SavePosition = savePosition;
        if (SavePosition)
        {
            var lastPosition = ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{id}"];
            if (lastPosition != null)
            {
                Axis.TargetPosition = (double)lastPosition;
            }
        }
    }
}

internal partial class StageSectionViewModel : ObservableObject
{
    public string Id { get; init; }
    public string Title { get; init; }

    StageTiltCompensationViewModel? TiltCompensation = null;

    public ObservableCollection<AxisViewModel> Axes { get; set; } = new();

    private static string[] AxisNames = new string[] { "X", "Y", "Z", "U", "V", "W" };

    private bool isLocked;
    public bool IsLocked
    {
        get => isLocked;
        set
        {
            isLocked = value;
            OnPropertyChanged(nameof(IsUnlocked));
            foreach (var axis in Axes)
            {
                axis.IsLocked = value;
            }
            ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{Id}.IsLocked"] = value;
        }
    }
    public bool IsUnlocked => !IsLocked;

    private float stepSize;
    public float StepSize
    {
        get => stepSize;
        set
        {
            stepSize = value;
            OnPropertyChanged(nameof(StepSize));
            foreach (var axis in Axes)
            {
                axis.StepSize = value;
            }
            ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{Id}.StepSize"] = value;
        }
    }

    public StageSectionViewModel(IEnumerable<(AxisComponent Axis, bool AxisInverted)> axes, string title, string id, bool enableTiltCompensation, bool savePosition)
    {
        Id = id;
        Title = title;
        foreach (var (ax, i) in axes.Select((ax, i) => (ax, i)))
        {
            Axes.Add(new AxisViewModel(ax.Axis, AxisNames[i], $"{id}-{AxisNames[i]}", ax.AxisInverted, savePosition));
        }
        if (enableTiltCompensation && Axes.Count >= 3)
        {
            TiltCompensation = new StageTiltCompensationViewModel(id, axes.Select(ax => ax.Axis));
        }
        IsLocked = (bool?)ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{Id}.IsLocked"] ?? false;
        StepSize = (float?)ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{Id}.StepSize"] ?? 1f;
    }

    public void Update()
    {
        foreach (var axis in Axes)
        {
            axis.UpdatePosition();
        }
    }

    GameControllerReading previousReading = GameController.GetReading();
    DateTime lastTimeStamp = DateTime.UtcNow;
    public void GameControllerUpdate(GameControllerReading reading)
    {
        var dt = Math.Min(0.1, (DateTime.UtcNow - lastTimeStamp).TotalSeconds);
        lastTimeStamp = DateTime.UtcNow;

        var moveBy = new double[] { 0, 0, 0 };
        for (var i = 0; i < Math.Min(Axes.Count, 3); i++)
        {
            var axisSign = Axes[i].AxisInverted ? -1 : 1;
            if (reading.Axis[i] != 0)
            {
                moveBy[i] += reading.Axis[i] * dt * 2000 * axisSign;
            }
            if (reading.AxisDiscrete[i] != 0 && previousReading.AxisDiscrete[i] == 0 && new[] { reading.A, reading.B, reading.Y }.All(x => x == false))
            {
                var speedUp = reading.X ? 10 : 1;
                moveBy[i] += reading.AxisDiscrete[i] * StepSize * speedUp * axisSign;
            }
            if (moveBy[i] != 0)
            {
                var remainingMove = Axes[i].Axis.TargetPosition - Axes[i].Axis.ActualPosition;
                if (Math.Abs(remainingMove) > Axes[i].Axis.MaxMoveSpeed * 0.25 && Math.Sign(remainingMove) == Math.Sign(moveBy[i]))
                {
                    // if the target is beyond reach within the next 0.25 s, don't move it further
                    moveBy[i] = 0;
                }
            }
            if (i == 2 && TiltCompensation?.CompensationGradients != null)
            {
                moveBy[i] += TiltCompensation.CompensationGradients[0] * moveBy[0] + TiltCompensation.CompensationGradients[1] * moveBy[1];
            }
            if (moveBy[i] != 0)
            {
                var currentPosition = Axes[i].TargetPosition;
                var newPosition = Math.Clamp(currentPosition + moveBy[i], Axes[i].MinPosition, Axes[i].MaxPosition); // In case the movement gets clamped...
                moveBy[i] = newPosition - currentPosition; // ...let's update `moveBy`
                Axes[i].TargetPosition = newPosition;
            }
        }
        previousReading = reading;
    }
}

[ObservableObject]
internal sealed partial class StageSectionControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(StageSectionViewModel), typeof(StageSectionControl), null);
    public StageSectionViewModel ViewModel { get => (StageSectionViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }

    public StageSectionControl()
    {
        this.InitializeComponent();
    }
}
