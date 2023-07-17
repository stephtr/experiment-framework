using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ExperimentFramework;

public record StageSection(IEnumerable<(int AxisIndex, bool AxisInverted)> Axes, string? Title = null, bool EnableTiltCompensation = true);

internal partial class StageControlViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMultipleSections))]
    [NotifyPropertyChangedFor(nameof(SectionViewsHeight))]
    private StageSectionViewModel[] sectionViews = new StageSectionViewModel[] { };

    public float SectionViewsHeight => 90f + SectionViews.Aggregate(0f, (oldMax, section) => Math.Max(oldMax, 36f * section.Axes.Count));

    public bool HasMultipleSections => SectionViews.Length > 1;

    public void UpdateSectionViews(IEnumerable<StageSection>? sections)
    {
        var stage = ExperimentContainer.Singleton.GetActiveComponent<StageComponent>();
        if (stage == null)
        {
            if (SectionViews.Length != 0)
            {
                SectionViews = new StageSectionViewModel[] { };
            }
            return;
        }
        var haveToSavePosition = !stage.MaintainsPosition;
        if (sections == null)
        {
            SectionViews = new StageSectionViewModel[] { new(stage.Axes.Select(ax => (ax, true)), "Stage", "stage", true, haveToSavePosition) };
            return;
        }

        SectionViews = sections.Select((s, i) =>
        {
            return new StageSectionViewModel(
                s.Axes.Select(ax =>
                    ax.AxisIndex < stage.Axes.Length ? (stage.Axes[ax.AxisIndex], ax.AxisInverted) : (null, true)
                ).Where(ax => ax.Item1 != null) as IEnumerable<(AxisComponent, bool)>,
                s.Title ?? "Stage", i.ToString(), s.EnableTiltCompensation, haveToSavePosition
            );
        }).ToArray();
    }
}

[ObservableObject]
public sealed partial class StageControl : UserControl
{
    public static readonly DependencyProperty SectionsProperty = DependencyProperty.Register("Sections", typeof(IEnumerable<StageSection>), typeof(StageControl), new PropertyMetadata(null, OnSectionsChanged));
    public IEnumerable<StageSection>? Sections { get => (IEnumerable<StageSection>?)GetValue(SectionsProperty); set => SetValue(SectionsProperty, value); }

    private static void OnSectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((StageControl)d).ViewModel.UpdateSectionViews(e.NewValue as IEnumerable<StageSection>);
    }

    private bool LoopRunning = true;
    public StageControl()
    {
        this.InitializeComponent();
        this.DataContext = new StageControlViewModel();

        ExperimentContainer.Singleton.AddComponentChangeHandler<StageComponent>((_) => ViewModel.UpdateSectionViews(Sections));
        ViewModel.UpdateSectionViews(Sections);

        async Task updateLoop()
        {
            while (LoopRunning)
            {
                foreach (var section in ViewModel.SectionViews)
                {
                    section.Update();
                }
                await Task.Delay(100);
            }
        }
        async Task controllerLoop()
        {
            var previousReading = new GameControllerReading();
            while (LoopRunning)
            {
                var reading = GameController.GetReading();
                var blankReading = new GameControllerReading();

                if (reading.A && reading.AxisDiscrete[2] != 0 && previousReading.AxisDiscrete[2] == 0 && ViewModel.SectionViews.Length > 1)
                {
                    SectionsFlipView.SelectedIndex = (SectionsFlipView.SelectedIndex + reading.AxisDiscrete[2] + ViewModel.SectionViews.Length) % ViewModel.SectionViews.Length;
                }

                foreach (var (section, index) in ViewModel.SectionViews.Select((item, index) => (item, index)))
                {
                    section.GameControllerUpdate(index == SectionsFlipView.SelectedIndex ? reading : blankReading);
                }

                previousReading = reading;
                await Task.Delay(20);
            }
        }
        _ = updateLoop();
        _ = controllerLoop();
    }

    internal StageControlViewModel ViewModel => (StageControlViewModel)DataContext;

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        LoopRunning = false;
    }
}
