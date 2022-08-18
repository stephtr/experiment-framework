namespace ExperimentFramework;

public abstract class ExperimentComponentClass : IDisposable
{
    public static string GetName(Type type)
    {
        return ((DisplayNameAttribute?)Attribute.GetCustomAttribute(type, typeof(DisplayNameAttribute)))?.Name ?? type.Name;
    }

    public static string GetIconString(Type type)
    {
        return ((IconStringAttribute?)Attribute.GetCustomAttribute(type, typeof(IconStringAttribute)))?.IconString ?? "";
    }

    /// <summary>Returns the settings type for a specific component type</summary>
    public static Type? GetSettingsType(Type? componentType)
    {
        if (componentType == null) return null;
        var constructors = componentType.GetConstructors();
        switch (constructors.Length)
        {
            case 0: return null;
            case 1: break;
            default: throw new Exception($"{componentType.Name} has too many constructors.");
        }
        var parameters = constructors[0].GetParameters();
        switch (parameters.Length)
        {
            case 0: return null;
            case 1: break;
            default: throw new Exception($"The constructor of {componentType.Name} can only take one settings argument.");
        }
        return parameters[0].ParameterType;
    }

    public virtual void Dispose() { }
}
