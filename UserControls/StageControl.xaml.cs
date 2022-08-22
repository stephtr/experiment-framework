using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.Storage;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Data;

namespace ExperimentFramework;

public class DoubleListToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var val = (IEnumerable<double>)value;
        return string.Join(" – ", val.Select(v => v.ToString("N3")));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class AxisViewModel : ObservableObject
{
    private AxisComponent Axis;
    public string Name { get; set; }
    public double MinPosition { get => Axis.MinPosition; }
    public double MaxPosition { get => Axis.MaxPosition; }
    public string ActualPositionFormatted { get => Axis.ActualPosition.ToString("N3"); }
    public double TargetPosition
    {
        get => Axis.TargetPosition;
        set
        {
            Axis.TargetPosition = Math.Clamp(value, MinPosition, MaxPosition);
            DebouncedSave(Name, Axis.TargetPosition);
            OnPropertyChanged(nameof(TargetPosition));
            OnPropertyChanged(nameof(ActualPositionFormatted));
        }
    }
    private Action<string, double> DebouncedSave = TaskUtils.Debounce((string name, double position) =>
    {
        ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{name.TrimEnd(':')}"] = position;
    });

    public void UpdatePosition()
    {
        OnPropertyChanged(nameof(ActualPositionFormatted));
    }

    public AxisViewModel(AxisComponent axis, string name)
    {
        Axis = axis;
        Name = name + ":";
        var lastPosition = ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings.Stage.{name}"];
        if (lastPosition != null)
        {
            Axis.TargetPosition = (double)lastPosition;
        }
    }
}

public partial class StageViewModel : ObservableObject
{
    private string[] AxisNames = new string[] { "X", "Y", "Z", "U", "V", "W" };
    public bool IsAvailable { get => Stage != null; }

    public ObservableCollection<AxisViewModel> Axes { get; set; } = new();

    private StageComponent? stage;
    public StageComponent? Stage
    {
        get => stage; set
        {
            SetProperty(ref stage, value);

            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(CanAddCompensationPoint));
            OnPropertyChanged(nameof(CanRefocus));
            OnPropertyChanged(nameof(CanReadjustCompensationOffset));
        }
    }

    public ObservableCollection<double[]> CompensationPoints { get; set; } = new();

    public bool CanAddCompensationPoint { get => Stage != null && CompensationPoints.Count < 3; }
    public bool CanRemoveCompensationPoint { get => CompensationPoints.Count > 0; }
    public bool CanReadjustCompensationOffset { get => CompensationNormVector != null && Stage != null; }
    public bool CanRefocus { get => Stage != null && CompensationNormVector != null; }
    public string CompensationFocusText { get => "Focus positions: " + (CompensationPoints.Count == 0 ? "none" : ""); }
    private string CompensationStatus = "";
    public string CompensationStatusText { get => "Status: " + (CompensationPoints.Count == 3 ? CompensationStatus : "not enough points"); }
    private double[]? CompensationNormVector;
    private double[]? CompensationGradients;

    [RelayCommand]
    public void AddCompensationPoint()
    {
        if (Stage != null)
        {
            var point = Stage.Axes.Select(ax => ax.TargetPosition).ToArray()!;
            var existingPoint = CompensationPoints.FirstOrDefault(p => p.Zip(point).Take(2).All((cs) => cs.First == cs.Second));
            if (existingPoint != null) // check for x-y duplicates
            {
                CompensationPoints.Remove(existingPoint);
            }
            CompensationPoints.Add(point);
            UpdateCompensation();
        }
    }

    [RelayCommand]
    public void RemoveCompensationPoint(ListView selectionList)
    {
        var items = selectionList.SelectedItems.Select(x => (x as double[])!).ToArray();
        if (items.Length > 0)
        {
            foreach (var item in items)
            {
                CompensationPoints.Remove(item);
            }
        }
        else
        {
            CompensationPoints.Clear();
        }
        UpdateCompensation();
    }

    [RelayCommand]
    public void ReadjustCompensationOffset()
    {
        if (Stage != null && CompensationNormVector != null)
        {
            CompensationNormVector[3] = CompensationNormVector[0] * Axes[0].TargetPosition + CompensationNormVector[1] * Axes[1].TargetPosition + CompensationNormVector[2] * Axes[2].TargetPosition;
            var pointsToBeUpdated = CompensationPoints.ToArray();
            foreach (var point in pointsToBeUpdated)
            {
                point[2] = (CompensationNormVector[3] - CompensationNormVector[0] * point[0] - CompensationNormVector[1] * point[1]) / CompensationNormVector[2];
                CompensationPoints[CompensationPoints.IndexOf(point)] = point;
            }
            UpdateCompensation();
        }
    }

    private void UpdateCompensation()
    {
        if (CompensationPoints.Count < 3)
        {
            CompensationNormVector = null;
            CompensationGradients = null;
        }
        else
        {
            var v1 = CompensationPoints[0].Zip(CompensationPoints[1], (x, y) => (double)(x - y)).ToArray();
            var v2 = CompensationPoints[0].Zip(CompensationPoints[2], (x, y) => (double)(x - y)).ToArray();
            var norm = new double[] { v1[1] * v2[2] - v1[2] * v2[1], v1[2] * v2[0] - v1[0] * v2[2], v1[0] * v2[1] - v1[1] * v2[0] };
            var length = Math.Sqrt(norm.Select(x => x * x).Sum());
            norm = norm.Select(x => x / length).ToArray();
            CompensationNormVector = norm.Append(norm.Zip(CompensationPoints[0], (x, y) => x * y).Sum()).ToArray();
            CompensationGradients = new double[] { -norm[0] / norm[2], -norm[1] / norm[2] };
            var angles = CompensationGradients.Select(t => Math.Atan(t) * 180 / Math.PI).ToList();
            if (angles.Any(angle => Math.Abs(angle) >= 10))
            {
                CompensationNormVector = null;
                CompensationGradients = null;
                CompensationStatus = "angle too large";
            }
            else
            {
                CompensationStatus = $"{angles[0]:0.00}° (X), {angles[1]:0.00}° (Y)";
            }
        }

        ApplicationData.Current.LocalSettings.SaveMatrix("Settings.ComponentSettings.Stage.Compensation", CompensationPoints);

        OnPropertyChanged(nameof(CanAddCompensationPoint));
        OnPropertyChanged(nameof(CanRemoveCompensationPoint));
        OnPropertyChanged(nameof(CompensationFocusText));
        OnPropertyChanged(nameof(CompensationStatusText));
        OnPropertyChanged(nameof(CanRefocus));
        OnPropertyChanged(nameof(CanReadjustCompensationOffset));
    }

    [RelayCommand]
    public void Refocus()
    {
        if (Stage != null && CompensationNormVector != null)
        {
            var z = (CompensationNormVector[3] - CompensationNormVector[0] * Axes[0].TargetPosition - CompensationNormVector[1] * Axes[1].TargetPosition) / CompensationNormVector[2];
            Axes[2].TargetPosition = z;
        }
    }

    private bool LoopRunning = true;
    public StageViewModel()
    {
        PropertyChanged += (o, e) =>
        {
            if (e.PropertyName == nameof(Stage))
            {
                Axes.Clear();
                if (Stage != null)
                {
                    for (var i = 0; i < Stage.Axes.Length; i++)
                    {
                        Axes.Add(new AxisViewModel(Stage.Axes[i], i < AxisNames.Length ? AxisNames[i] : ""));
                    }
                }
            }
        };

        ApplicationData.Current.LocalSettings.LoadMatrix("Settings.ComponentSettings.Stage.Compensation", (point) => CompensationPoints.Add(point));
        UpdateCompensation();

        var updateLoop = async () =>
        {
            while (LoopRunning)
            {
                foreach (var axis in Axes)
                {
                    axis.UpdatePosition();
                }
                OnPropertyChanged(nameof(Axes));
                await Task.Delay(100);
            }
        };
        var controllerLoop = async () =>
        {
            var previousReading = GameController.GetReading();
            var lastTimeStamp = DateTime.UtcNow;
            var stepSizes = new double[] { 0.5, 0.5, 0.02 }; // µm
            while (LoopRunning)
            {
                var reading = GameController.GetReading();
                var dt = Math.Min(0.1, (DateTime.UtcNow - lastTimeStamp).TotalSeconds);
                lastTimeStamp = DateTime.UtcNow;

                var moveBy = new double[] { 0, 0, 0 };
                for (var i = 0; i < Math.Min(Axes.Count, 3); i++)
                {
                    if (i == 2 && CompensationGradients != null)
                    {
                        moveBy[i] += CompensationGradients[0] * moveBy[0] + CompensationGradients[1] * moveBy[1];
                    }
                    if (reading.Axis[i] != 0)
                    {
                        moveBy[i] += reading.Axis[i] * dt * 200;
                    }
                    if (reading.AxisDiscrete[i] != 0 && previousReading.AxisDiscrete[i] == 0)
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
                await Task.Delay(20);
            }
        };
        updateLoop();
        controllerLoop();
    }

    public void Unload()
    {
        LoopRunning = false;
    }
}

public sealed partial class StageControl : UserControl
{
    public StageComponent? Stage
    {
        get => (StageComponent)GetValue(StageProperty);
        set => SetValue(StageProperty, value);
    }
    public static readonly DependencyProperty StageProperty = DependencyProperty.Register("Stage", typeof(StageComponent), typeof(StageControl), new PropertyMetadata(null, StageChanged));

    private static void StageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StageControl)d;
        ((StageViewModel)control.DataContext).Stage = (StageComponent?)e.NewValue;
    }

    public StageControl()
    {
        this.InitializeComponent();
    }

    private void StageControl_Unloaded(object sender, RoutedEventArgs e)
    {
        ((StageViewModel)DataContext).Unload();
    }
}
