using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace ExperimentFramework;

internal record NameDisplayName(string? Name, string DisplayName);

[ObservableObject]
internal partial class ComponentSettingsData
{
    private ExperimentContainer Container { get; init; }
    public Type ComponentClass { get; init; }
    public string ClassId { get; init; }
    public string Name { get; init; }
    public string Icon { get; init; }
    public IEnumerable<NameDisplayName> PotentialComponents;
    public string PickAnAlternativeText => $"Pick a {Name} type";
    public string? SelectedComponent
    {
        get => Container.GetActiveComponentName(ComponentClass, ClassId);
        set
        {
            if (SelectedComponent == value)
            {
                return;
            }
            OnPropertyChanging(nameof(Settings));
            object? settings = null;
            if (value != null)
            {
                var componentType = Container.GetComponentTypeFromName(value);
                var settingsType = ExperimentComponentClass.GetSettingsType(componentType);
                if (settingsType != null)
                {
                    settings = Activator.CreateInstance(settingsType);
                }
            }
            Container.ActivateComponentAsync(ComponentClass, ClassId, value, settings);
            FreezeSettingsUpdates = true;
            OnPropertyChanged(nameof(Settings));
            FreezeSettingsUpdates = false;
        }
    }
    private bool FreezeSettingsUpdates = false;
    public object? Settings
    {
        get => Container.GetActiveComponentSettings(ComponentClass, ClassId);
        set
        {
            if (!FreezeSettingsUpdates)
            {
                Container.ActivateComponentAsync(ComponentClass, ClassId, SelectedComponent, value);
            }
        }
    }

    public ComponentSettingsData(ExperimentContainer container, Type componentClass, string classId)
    {
        Container = container;
        ComponentClass = componentClass;
        ClassId = classId;
        Name = ExperimentComponentClass.GetName(componentClass);
        Icon = ExperimentComponentClass.GetIconString(componentClass);
        PotentialComponents = container.GetComponents(componentClass).Select(x => new NameDisplayName(x.Name, x.DisplayName)).Prepend(new NameDisplayName(null, "Disabled"));
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

        settingsPanel.ComponentsData.Clear();
        var componentDataToAdd = container.GetComponentClasses().Select(entry =>
            new ComponentSettingsData(container, entry.Class, entry.Id));
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
