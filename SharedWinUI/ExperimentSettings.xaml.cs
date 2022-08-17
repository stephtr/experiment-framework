using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace ExperimentFramework;

internal record NameDisplayName(string Name, string DisplayName);

internal class ComponentSettingsData
{
    public Type ComponentClass { get; init; }
    public string Name { get; init; }
    public string Icon { get; init; }
    public IEnumerable<NameDisplayName> PotentialComponents;
    public string PickAnAlternativeText => $"Pick a {Name} type";

    public ComponentSettingsData(ExperimentContainer container, Type componentCategory)
    {
        ComponentClass = componentCategory;
        Name = ExperimentComponentClass.GetName(componentCategory);
        Icon = ExperimentComponentClass.GetIconString(componentCategory);
        PotentialComponents = container.GetComponents(componentCategory).Select(x => new NameDisplayName(x.Name, x.DisplayName)).Prepend(new NameDisplayName("", "Disabled"));
    }
}

public sealed partial class ExperimentSettings : UserControl
{
    ObservableCollection<ComponentSettingsData> ComponentsData = new();

    public ExperimentContainer Container
    {
        get => (ExperimentContainer)GetValue(ContainerProperty);
        set => SetValue(ContainerProperty, value);
    }
    public static readonly DependencyProperty ContainerProperty = DependencyProperty.Register(nameof(Container), typeof(ExperimentContainer), typeof(ExperimentSettings), new PropertyMetadata(null, OnContainerChanged));
    private static void OnContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var settingsPanel = (ExperimentSettings)d;
        var container = (ExperimentContainer)e.NewValue;

        container.ComponentChanged += (componentClass, id, component) =>
        {
            /*for (var i = 0; i < settingsPanel.ComponentsData.Count; i++)
            {
                if (settingsPanel.ComponentsData[i].ComponentCategory == componentClass)
                {
                    settingsPanel.ComponentsData[i] = new ComponentStatusBarData(componentClass, component);
                    return;
                }
            }
            settingsPanel.ComponentsData.Add(new ComponentStatusBarData(componentClass, component));*/
        };

        settingsPanel.ComponentsData.Clear();
        var componentDataToAdd = container.GetComponentClasses().Select(componentCategory =>
            new ComponentSettingsData(container, componentCategory));
        foreach (var data in componentDataToAdd)
        {
            settingsPanel.ComponentsData.Add(data);
        }
    }

    public ExperimentSettings()
    {
        this.InitializeComponent();
    }
}
