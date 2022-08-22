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

public static class ApplicationDataUtils
{
    public static void SaveMatrix(this ApplicationDataContainer container, string key, IEnumerable<double[]> value)
    {
        var i1 = 0;
        foreach (var point in value)
        {
            var i2 = 0;
            foreach (var c in point)
            {
                container.Values[$"{key}.{i1}_{i2}"] = c;
                i2++;
            }
            for (; container.Values.Remove($"{key}.{i1}_{i2}"); i2++) ;
            i1++;
        }
        for (; container.Values.Remove($"{key}.{i1}_0"); i1++)
        { // remove surplus points
            for (var i2 = 1; container.Values.Remove($"{key}.{i1}_{i2}"); i2++) ;
        }
    }

    public static void LoadMatrix(this ApplicationDataContainer container, string key, Action<double[]> callback)
    {
        for (var i1 = 0; ; i1++)
        {
            var p = new List<double>();
            for (var i2 = 0; ; i2++)
            {
                var val = container.Values[$"{key}.{i1}_{i2}"] as double?;
                if (!val.HasValue) break;
                p.Add(val.Value);
            }
            if (p.Count == 0) break;
            callback(p.ToArray());
        }
    }
}
