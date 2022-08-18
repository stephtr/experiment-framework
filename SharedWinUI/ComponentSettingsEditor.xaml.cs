using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace ExperimentFramework;

internal class SettingItem : ObservableObject
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public SettingItem(string name, string? displayName = null)
    {
        Name = name;
        DisplayName = displayName ?? name;
    }
}
internal partial class StringSettingItem : SettingItem
{
    [ObservableProperty]
    private string currentValue;
    public StringSettingItem(string name, string currentValue, string? displayName = null) : base(name, displayName)
    {
        this.currentValue = currentValue;
    }
}
internal partial class BoolSettingItem : SettingItem
{
    [ObservableProperty]
    private bool currentValue;
    public BoolSettingItem(string name, bool currentValue, string? displayName = null) : base(name, displayName)
    {
        this.currentValue = currentValue;
    }
}
internal partial class IntSettingItem : SettingItem
{
    [ObservableProperty]
    private int currentValue;
    public IntSettingItem(string name, int currentValue, string? displayName = null) : base(name, displayName)
    {
        this.currentValue = currentValue;
    }
}
internal partial class DoubleSettingItem : SettingItem
{
    [ObservableProperty]
    private double currentValue;
    public DoubleSettingItem(string name, double currentValue, string? displayName = null) : base(name, displayName)
    {
        this.currentValue = currentValue;
    }
}
internal partial class StringListSettingItem : SettingItem
{
    public IEnumerable<string> Options { get; set; }
    [ObservableProperty]
    private string currentValue;
    public StringListSettingItem(string name, IEnumerable<string> options, string currentValue, string? displayName = null) : base(name, displayName)
    {
        Options = options;
        this.currentValue = currentValue;
    }
}

internal class SettingDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate StringTemplate { get; set; } = null!;
    public DataTemplate BoolTemplate { get; set; } = null!;
    public DataTemplate IntTemplate { get; set; } = null!;
    public DataTemplate StringListTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item) =>
        item switch
        {
            StringSettingItem => StringTemplate,
            BoolSettingItem => BoolTemplate,
            IntSettingItem => IntTemplate,
            DoubleSettingItem => IntTemplate,
            StringListSettingItem => StringListTemplate,
            _ => base.SelectTemplateCore(item),
        };

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}

internal sealed partial class ComponentSettingsEditor : UserControl
{
    public object? Settings
    {
        get => GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register("Settings", typeof(object), typeof(ComponentSettingsEditor), new PropertyMetadata(null, SettingsChanged));

    private static void SettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue?.GetType() != e.NewValue?.GetType())
        {
            (d as ComponentSettingsEditor)?.RebuildControl();
        }
    }

    public ObservableCollection<SettingItem> SettingEntries = new();

    public ComponentSettingsEditor()
    {
        this.InitializeComponent();
        SettingEntries.CollectionChanged += SettingEntries_CollectionChanged;
    }

    private bool FreezeSettingsChangeNotifications = false;
    private void SettingEntries_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (!FreezeSettingsChangeNotifications)
        {
            Settings = GetUpdatedSettings();
        }
    }

    public void RebuildControl()
    {
        FreezeSettingsChangeNotifications = true;
        SettingEntries.Clear();
        var SettingsType = Settings?.GetType();
        var properties = SettingsType?.GetProperties();
        if (properties == null) return;
        foreach (var property in properties)
        {
            if (property.GetSetMethod()?.IsStatic ?? true) continue;
            var optionsProperty = (Attribute.GetCustomAttribute(property, typeof(OptionsAttribute)) as OptionsAttribute)?.optionsProperty;
            var displayName = (Attribute.GetCustomAttribute(property, typeof(DisplayNameAttribute)) as DisplayNameAttribute)?.Name;
            SettingItem? item = null;
            if (property.PropertyType == typeof(string) && optionsProperty != null)
            {
                var options = properties.SingleOrDefault(p => p.Name == optionsProperty)?.GetValue(null) as IEnumerable<string> ?? new string[] { };
                item = new StringListSettingItem(property.Name, options, property.GetValue(Settings) as string ?? "", displayName);
            }
            else if (property.PropertyType == typeof(bool))
            {
                item = new BoolSettingItem(property.Name, property.GetValue(Settings) as bool? ?? false, displayName);
            }
            else if (property.PropertyType == typeof(int))
            {
                item = new IntSettingItem(property.Name, property.GetValue(Settings) as int? ?? 0, displayName);
            }
            else if (property.PropertyType == typeof(double))
            {
                item = new DoubleSettingItem(property.Name, property.GetValue(Settings) as int? ?? 0, displayName);
            }
            else
            {
                item = new StringSettingItem(property.Name, property.GetValue(Settings) as string ?? "", displayName);
            }
            SettingEntries.Add(item);
            item.PropertyChanged += (sender, e) => SettingEntries[SettingEntries.IndexOf(item)] = item;
        }
        FreezeSettingsChangeNotifications = false;
    }

    private object? GetUpdatedSettings()
    {
        var SettingsType = Settings?.GetType();
        if (SettingsType == null) return null;
        var settings = Activator.CreateInstance(SettingsType);
        foreach (var entry in SettingEntries)
        {
            object value = entry switch
            {
                StringSettingItem item => item.CurrentValue,
                BoolSettingItem item => item.CurrentValue,
                IntSettingItem item => item.CurrentValue,
                DoubleSettingItem item => item.CurrentValue,
                StringListSettingItem item => item.CurrentValue,
                _ => throw new ArgumentException("Unknown settings entry type"),
            };
            SettingsType.GetProperty(entry.Name)?.SetValue(settings, value);
        }
        return settings;
    }
}
