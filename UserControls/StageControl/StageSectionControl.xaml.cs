using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace ExperimentFramework;

internal class AxisViewModel : ObservableObject
{
    public AxisComponent Axis { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public bool PositiveDirection { get; set; }
    public double MinPosition { get => Axis.MinPosition; }
    public double MaxPosition { get => Axis.MaxPosition; }
    public string ActualPositionFormatted { get => Axis.ActualPosition.ToString("N3"); }
    public double TargetPosition
    {
        get => Axis.TargetPosition;
        set
        {
            Axis.TargetPosition = Math.Clamp(value, MinPosition, MaxPosition);
            DebouncedSave(Id, Axis.TargetPosition);
            OnPropertyChanged(nameof(TargetPosition));
            OnPropertyChanged(nameof(ActualPositionFormatted));
        }
    }
    private Action<string, double> DebouncedSave = TaskUtils.Debounce((string id, double position) =>
    {
        ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{id}"] = position;
    });

    public void UpdatePosition()
    {
        OnPropertyChanged(nameof(ActualPositionFormatted));
    }

    public AxisViewModel(AxisComponent axis, string name, string id, bool positiveDirection)
    {
        Axis = axis;
        Id = id;
        Name = name + ":";
        PositiveDirection = positiveDirection;
        var lastPosition = ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{id}"];
        if (lastPosition != null)
        {
            Axis.TargetPosition = (double)lastPosition;
        }
    }
}

internal partial class StageSectionViewModel : ObservableObject
{
    public string Title { get; init; }

    StageTiltCompensationViewModel? TiltCompensation = null;

    public ObservableCollection<AxisViewModel> Axes { get; set; } = new();

    private static string[] AxisNames = new string[] { "X", "Y", "Z", "U", "V", "W" };

    public StageSectionViewModel(IEnumerable<(AxisComponent Axis, bool PositiveDirection)> axes, string title, string id, bool enableTiltCompensation)
    {
        Title = title;
        foreach (var (ax, i) in axes.Select((ax, i) => (ax, i)))
        {
            Axes.Add(new AxisViewModel(ax.Axis, AxisNames[i], $"{id}-{AxisNames[i]}", ax.PositiveDirection));
        }
        if (enableTiltCompensation && Axes.Count >= 3)
        {
            TiltCompensation = new StageTiltCompensationViewModel(id, axes.Select(ax => ax.Axis));
        }
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
        var stepSizes = new double[] { 0.5, 0.5, 0.02 }; // µm

        var dt = Math.Min(0.1, (DateTime.UtcNow - lastTimeStamp).TotalSeconds);
        lastTimeStamp = DateTime.UtcNow;

        var moveBy = new double[] { 0, 0, 0 };
        for (var i = 0; i < Math.Min(Axes.Count, 3); i++)
        {
            if (i == 2 && TiltCompensation?.CompensationGradients != null)
            {
                moveBy[i] += TiltCompensation.CompensationGradients[0] * moveBy[0] + TiltCompensation.CompensationGradients[1] * moveBy[1];
            }
            if (reading.Axis[i] != 0)
            {
                moveBy[i] += reading.Axis[i] * dt * 200;
            }
            if (reading.AxisDiscrete[i] != 0 && previousReading.AxisDiscrete[i] == 0 && new[] { reading.A, reading.B, reading.X, reading.Y }.All(x => x == false))
            {
                var speed = reading.X ? 10 : 1;
                moveBy[i] += reading.AxisDiscrete[i] * stepSizes[i] * speed;
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
