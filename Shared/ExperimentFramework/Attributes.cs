namespace ExperimentFramework;

/// <summary>This attribute can be used to give components and setting properties a custom display name</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class DisplayNameAttribute : Attribute
{
    public readonly string Name;
    public DisplayNameAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// For component types (like the `LaserComponent` class), this attribute represents the component icon as Unicode string;
/// For available icons see https://docs.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IconStringAttribute : Attribute
{
    public readonly string IconString;
    public IconStringAttribute(string iconString)
    {
        IconString = iconString;
    }
}

/// <summary>For component settings, this attribute renders a select box with the available options instead of a text box</summary>
[AttributeUsage(AttributeTargets.Property)]
public class OptionsAttribute : Attribute
{
    public string optionsProperty;
    public OptionsAttribute(string optionsProperty)
    {
        this.optionsProperty = optionsProperty;
    }
}
