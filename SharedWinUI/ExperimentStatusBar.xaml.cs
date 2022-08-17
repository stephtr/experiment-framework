using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace ExperimentFramework;

internal record ComponentStatusBarData
{
    public Type ComponentCategory { get; init; }
    public string Name { get; init; }
    public string Icon { get; init; }
    public string? ActiveComponentName { get; init; }
    public string NameWithDescription => ActiveComponentName == null ? Name : $"{Name}: {ActiveComponentName}";
    public Brush BackgroundBrush { get; init; }

    public ComponentStatusBarData(Type componentCategory, ExperimentComponentClass? activeComponent)
    {
        ComponentCategory = componentCategory;
        Name = ExperimentComponentClass.GetName(componentCategory);
        Icon = ExperimentComponentClass.GetIconString(componentCategory);
        ActiveComponentName = activeComponent == null ? null : ExperimentComponentClass.GetName(activeComponent.GetType());
        BackgroundBrush = new SolidColorBrush(activeComponent == null ? Colors.DarkRed : Colors.DarkGreen);
    }
}

public sealed partial class ExperimentStatusBar : UserControl
{
    ObservableCollection<ComponentStatusBarData> ComponentsData = new();

    public ExperimentContainer Container
    {
        get => (ExperimentContainer)GetValue(ContainerProperty);
        set => SetValue(ContainerProperty, value);
    }
    public static readonly DependencyProperty ContainerProperty = DependencyProperty.Register(nameof(Container), typeof(ExperimentContainer), typeof(ExperimentStatusBar), new PropertyMetadata(null, OnContainerChanged));
    private static void OnContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var statusBar = (ExperimentStatusBar)d;
        var container = (ExperimentContainer)e.NewValue;

        container.ComponentChanged += (category, component) =>
        {
            for (var i = 0; i < statusBar.ComponentsData.Count; i++)
            {
                if (statusBar.ComponentsData[i].ComponentCategory == category)
                {
                    statusBar.ComponentsData[i] = new ComponentStatusBarData(category, component);
                    return;
                }
            }
            statusBar.ComponentsData.Add(new ComponentStatusBarData(category, component));
        };

        statusBar.ComponentsData.Clear();
        var componentDataToAdd = container.GetComponentClasses().Select(componentCategory =>
            new ComponentStatusBarData(componentCategory, container.GetActiveComponent(componentCategory)));
        foreach (var data in componentDataToAdd)
        {
            statusBar.ComponentsData.Add(data);
        }
    }

    public ExperimentStatusBar()
    {
        this.InitializeComponent();
    }
}
