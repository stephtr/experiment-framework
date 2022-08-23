using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace ExperimentFramework;

internal partial class ComponentStatusBarData
{
    public Type ComponentClass { get; init; }
    public string ClassId { get; set; }
    public string Name { get; init; }
    public string Icon { get; init; }
    public string? ActiveComponentName { get; init; }
    public string NameWithDescription => ActiveComponentName == null ? Name : $"{Name}: {ActiveComponentName}";
    public Brush BackgroundBrush { get; init; }

    public ComponentStatusBarData(Type componentClass, string classId, ExperimentComponentClass? activeComponent)
    {
        ComponentClass = componentClass;
        ClassId = classId;
        Name = ExperimentComponentClass.GetName(componentClass);
        Icon = ExperimentComponentClass.GetIconString(componentClass);
        ActiveComponentName = activeComponent == null ? null : ExperimentComponentClass.GetName(activeComponent.GetType());
        BackgroundBrush = new SolidColorBrush(activeComponent == null ? Colors.DarkRed : Colors.DarkGreen);
    }

    public void ReloadComponent()
    {
        ExperimentContainer.Singleton.ReloadComponent(ComponentClass, ClassId);
    }
}

public sealed partial class ExperimentStatusBar : UserControl
{
    ObservableCollection<ComponentStatusBarData> ComponentsData = new();

    public ExperimentStatusBar()
    {
        this.InitializeComponent();

        var componentDataToAdd = ExperimentContainer.Singleton.GetComponentClasses().Select((entry) =>
            new ComponentStatusBarData(entry.Class, entry.Id, ExperimentContainer.Singleton.GetActiveComponent(entry.Class, entry.Id)));
        foreach (var data in componentDataToAdd)
        {
            ComponentsData.Add(data);
        }

        ExperimentContainer.Singleton.ComponentChanged += (componentClass, id, component) =>
        {
            for (var i = 0; i < ComponentsData.Count; i++)
            {
                if (ComponentsData[i].ComponentClass == componentClass)
                {
                    ComponentsData[i] = new ComponentStatusBarData(componentClass, id, component);
                    return;
                }
            }
            ComponentsData.Add(new ComponentStatusBarData(componentClass, id, component));
        };
    }
}
