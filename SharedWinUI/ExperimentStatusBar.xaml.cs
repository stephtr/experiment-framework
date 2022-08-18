using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace ExperimentFramework;

internal partial class ComponentStatusBarData
{
    ExperimentContainer Container { get; init; }
    public Type ComponentClass { get; init; }
    public string ClassId { get; set; }
    public string Name { get; init; }
    public string Icon { get; init; }
    public string? ActiveComponentName { get; init; }
    public string NameWithDescription => ActiveComponentName == null ? Name : $"{Name}: {ActiveComponentName}";
    public Brush BackgroundBrush { get; init; }

    public ComponentStatusBarData(ExperimentContainer container, Type componentClass, string classId, ExperimentComponentClass? activeComponent)
    {
        Container = container;
        ComponentClass = componentClass;
        ClassId = classId;
        Name = ExperimentComponentClass.GetName(componentClass);
        Icon = ExperimentComponentClass.GetIconString(componentClass);
        ActiveComponentName = activeComponent == null ? null : ExperimentComponentClass.GetName(activeComponent.GetType());
        BackgroundBrush = new SolidColorBrush(activeComponent == null ? Colors.DarkRed : Colors.DarkGreen);
    }

    public void ReloadComponent()
    {
        Container.ReloadComponent(ComponentClass, ClassId);
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

        container.ComponentChanged += (componentClass, id, component) =>
        {
            for (var i = 0; i < statusBar.ComponentsData.Count; i++)
            {
                if (statusBar.ComponentsData[i].ComponentClass == componentClass)
                {
                    statusBar.ComponentsData[i] = new ComponentStatusBarData(container, componentClass, id, component);
                    return;
                }
            }
            statusBar.ComponentsData.Add(new ComponentStatusBarData(container, componentClass, id, component));
        };

        statusBar.ComponentsData.Clear();
        var componentDataToAdd = container.GetComponentClasses().Select((entry) =>
            new ComponentStatusBarData(container, entry.Class, entry.Id, container.GetActiveComponent(entry.Class, entry.Id)));
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
