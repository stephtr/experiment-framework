using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace ExperimentFramework;

internal record CompensationPoint(double[] Coords)
{
    public string AsFormattedString => string.Join(" – ", Coords.Select(v => v.ToString("N3")));
}

internal partial class StageTiltCompensationViewModel : ObservableObject
{
    private readonly string Id;
    private readonly AxisComponent[] Axes;
    public StageTiltCompensationViewModel(string id, IEnumerable<AxisComponent> axes)
    {
        Id = id;
        Axes = axes.ToArray();
        if (Axes.Length < 3)
        {
            throw new ArgumentException(nameof(axes), "At least 3 axes are necessary for tilt compensation.");
        }
        ApplicationData.Current.LocalSettings.LoadMatrix($"Settings.ComponentSettings.Stage.{Id}.Compensation", (point) => CompensationPoints.Add(new(point)));
        UpdateCompensation();
    }

    public ObservableCollection<CompensationPoint> CompensationPoints { get; set; } = new();

    public bool CanAddCompensationPoint { get => CompensationPoints.Count < 3; }
    public bool CanRemoveCompensationPoint { get => CompensationPoints.Count > 0; }
    public bool CanReadjustCompensationOffset { get => CompensationNormVector != null; }
    public bool CanRefocus { get => CompensationNormVector != null; }
    public string CompensationFocusText { get => "Focus positions: " + (CompensationPoints.Count == 0 ? "none" : ""); }
    private string CompensationStatus = "";
    public string CompensationStatusText { get => "Status: " + (CompensationPoints.Count == 3 ? CompensationStatus : "not enough points"); }
    private double[]? CompensationNormVector;
    public double[]? CompensationGradients;

    [RelayCommand]
    public void AddCompensationPoint()
    {
        var point = Axes.Select(ax => ax.TargetPosition).ToArray()!;
        var existingPoint = CompensationPoints.FirstOrDefault(p => p.Coords.Zip(point).Take(2).All((cs) => cs.First == cs.Second));
        if (existingPoint != null) // check for x-y duplicates
        {
            CompensationPoints.Remove(existingPoint);
        }
        CompensationPoints.Add(new(point));
        UpdateCompensation();
    }

    [RelayCommand]
    public void RemoveCompensationPoint(ListView selectionList)
    {
        var items = selectionList.SelectedItems.Select(x => (x as CompensationPoint)!).ToArray();
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
        if (CompensationNormVector == null) return;
        CompensationNormVector[3] = CompensationNormVector[0] * Axes[0].TargetPosition + CompensationNormVector[1] * Axes[1].TargetPosition + CompensationNormVector[2] * Axes[2].TargetPosition;
        var pointsToBeUpdated = CompensationPoints.ToArray();
        foreach (var point in pointsToBeUpdated)
        {
            point.Coords[2] = (CompensationNormVector[3] - CompensationNormVector[0] * point.Coords[0] - CompensationNormVector[1] * point.Coords[1]) / CompensationNormVector[2];
            CompensationPoints[CompensationPoints.IndexOf(point)] = point;
        }
        UpdateCompensation();
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
            var v1 = CompensationPoints[0].Coords.Zip(CompensationPoints[1].Coords, (x, y) => (double)(x - y)).ToArray();
            var v2 = CompensationPoints[0].Coords.Zip(CompensationPoints[2].Coords, (x, y) => (double)(x - y)).ToArray();
            var norm = new double[] { v1[1] * v2[2] - v1[2] * v2[1], v1[2] * v2[0] - v1[0] * v2[2], v1[0] * v2[1] - v1[1] * v2[0] };
            var length = Math.Sqrt(norm.Select(x => x * x).Sum());
            norm = norm.Select(x => x / length).ToArray();
            CompensationNormVector = norm.Append(norm.Zip(CompensationPoints[0].Coords, (x, y) => x * y).Sum()).ToArray();
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

        ApplicationData.Current.LocalSettings.SaveMatrix($"Settings.ComponentSettings.Stage.{Id}.Compensation", CompensationPoints.Select(p => p.Coords));

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
        if (CompensationNormVector != null)
        {
            var z = (CompensationNormVector[3] - CompensationNormVector[0] * Axes[0].TargetPosition - CompensationNormVector[1] * Axes[1].TargetPosition) / CompensationNormVector[2];
            Axes[2].TargetPosition = z;
        }
    }
}

internal sealed partial class StageTiltCompensationControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(StageTiltCompensationViewModel), typeof(StageSectionControl), null);
    public StageTiltCompensationViewModel ViewModel { get => (StageTiltCompensationViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }

    public StageTiltCompensationControl()
    {

        this.InitializeComponent();
    }
}
