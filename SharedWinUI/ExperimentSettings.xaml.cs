using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace ExperimentFramework;

internal record NameDisplayName(string? Name, string DisplayName);

[ObservableObject]
internal partial class ComponentSettingsData
{
    public Type ComponentClass { get; init; }
    public string ClassId { get; init; }
    public string Name { get; init; }
    public string Icon { get; init; }
    public IEnumerable<NameDisplayName> PotentialComponents;
    public string PickAnAlternativeText => $"Pick a {Name} type";
    public string? SelectedComponent
    {
        get => ExperimentContainer.Singleton.GetActiveComponentName(ComponentClass, ClassId);
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
                var componentType = ExperimentContainer.Singleton.GetComponentTypeFromName(value);
                var settingsType = ExperimentComponentClass.GetSettingsType(componentType);
                if (settingsType != null)
                {
                    settings = Activator.CreateInstance(settingsType);
                }
            }
            ExperimentContainer.Singleton.ActivateComponentAsync(ComponentClass, ClassId, value, settings);
            FreezeSettingsUpdates = true;
            OnPropertyChanged(nameof(Settings));
            FreezeSettingsUpdates = false;
        }
    }
    private bool FreezeSettingsUpdates = false;
    public object? Settings
    {
        get => ExperimentContainer.Singleton.GetActiveComponentSettings(ComponentClass, ClassId);
        set
        {
            if (!FreezeSettingsUpdates)
            {
                ExperimentContainer.Singleton.ActivateComponentAsync(ComponentClass, ClassId, SelectedComponent, value);
            }
        }
    }

    public ComponentSettingsData(Type componentClass, string classId)
    {
        ComponentClass = componentClass;
        ClassId = classId;
        Name = ExperimentComponentClass.GetName(componentClass);
        Icon = ExperimentComponentClass.GetIconString(componentClass);
        PotentialComponents = ExperimentContainer.Singleton.GetComponents(componentClass).Select(x => new NameDisplayName(x.Name, x.DisplayName)).Prepend(new NameDisplayName(null, "Disabled"));
    }
}

public sealed partial class ExperimentSettings : UserControl
{
    ObservableCollection<ComponentSettingsData> ComponentsData = new();

    public ExperimentSettings()
    {
        this.InitializeComponent();

        var componentDataToAdd = ExperimentContainer.Singleton.GetComponentClasses().Select(entry =>
            new ComponentSettingsData(entry.Class, entry.Id));
        foreach (var data in componentDataToAdd)
        {
            ComponentsData.Add(data);
        }
    }
}
