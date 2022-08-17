using Windows.Storage;

namespace ExperimentFramework;

public static class SettingsStore
{
    /*public static object? GetSettingsForType(this ExperimentContainer container, Type componentClass, string? componentString)
    {
        if (componentString == null) return null;

        var settingsType = container.GetSettingsType(componentClass, componentString);
        return settingsType == null ? null : GetComponentSettings(settingsType);
    }

    public static void InitFromSettings(this ExperimentContainer container)
    {
        container.InitFrom((type) =>
        {
            var selectedComponentString = (string?)ApplicationData.Current.LocalSettings.Values[$"Settings.SelectedComponent[{componentClass}]"];
            if (selectedComponentString is null)
            {
                return null;
            }

            var settings = GetSettingsForType(container, componentClass, componentString);
            return (selectedComponentString, settings);
        });
    }*/
}
