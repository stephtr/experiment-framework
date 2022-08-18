using System.Reflection;
using Windows.Storage;

namespace ExperimentFramework;

public static class SettingsStore
{
    public static void UseWinUISettingsStore(this ExperimentContainer container)
    {
        container.SettingsSaveHandler = SaveSettings;
        container.SettingsLoadHandler = LoadSettings;
    }

    private static void SaveSettings(ExperimentContainer sender, string containerId, string? activeComponentName, object? settings)
    {
        ApplicationData.Current.LocalSettings.Values[$"Settings.SelectedComponent[{containerId}]"] = activeComponentName;
        if (settings == null)
        {
            return;
        }
        var settingsType = settings.GetType();
        foreach (var property in settingsType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings[{containerId}][{property.Name}]"] = property.GetValue(settings);
        }
    }

    private static (string? ActiveComponentName, object? Settings) LoadSettings(ExperimentContainer sender, string containerId)
    {
        var activeComponentName = (string?)ApplicationData.Current.LocalSettings.Values[$"Settings.SelectedComponent[{containerId}]"];
        if (activeComponentName == null)
        {
            return (null, null);
        }

        var settingsType = ExperimentComponentClass.GetSettingsType(sender.GetComponentTypeFromName(activeComponentName));
        if (settingsType == null)
        {
            return (activeComponentName, null);
        }

        var settings = Activator.CreateInstance(settingsType);
        foreach (var property in settingsType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var value = ApplicationData.Current.LocalSettings.Values[$"Settings.ComponentSettings[{containerId}][{property.Name}]"];
            if (value != null)
            {
                try
                {
                    property.SetValue(settings, value);
                }
                catch { }
            }
        }
        return (activeComponentName, settings);
    }
}
