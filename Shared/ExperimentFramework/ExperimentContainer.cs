namespace ExperimentFramework;

internal class ComponentClassEntry
{
    public Type Class { get; set; }
    public string Id { get; set; }
    public ExperimentComponentClass? ActiveComponent { get; set; }
    public Action<ExperimentComponentClass?> ChangeHandler { get; set; }
    public ComponentClassEntry(Type className, string id, ExperimentComponentClass? activeComponent, Action<ExperimentComponentClass?> changeHandler)
    {
        Class = className;
        Id = id;
        ActiveComponent = activeComponent;
        ChangeHandler = changeHandler;
    }
}

public class ExperimentContainer : IDisposable
{
    /// <summary>Available component classes, like `LaserComponent`</summary>
    private readonly List<ComponentClassEntry> ComponentClasses = new();
    /// <summary>A list of possible implementations</summary>
    private readonly Dictionary<Type, List<Type>> Components = new(); // a list of possible implementations
    public event Action<Type, string, ExperimentComponentClass?> ComponentChanged = delegate { };

    /// <summary>Adds the component class `T`, like `LaserComponent`</summary>
    public ExperimentContainer AddComponentClass<T>(string? id = null) where T : ExperimentComponentClass
    {
        var exists = (string id) => ComponentClasses.Any(c => c.Id == id);

        var type = typeof(T);
        if (id == null)
        {
            id = ExperimentComponentClass.GetName(type);
            if (exists(id))
            {
                var i = 2;
                for (; exists(id); i++) ;
                id = $"{id} ({i})";
            }
        }
        else if (exists(id))
        {
            throw new ArgumentException($"Component class {type} with id {id} already exists");
        }
        ComponentClasses.Add(new(type, id, null, delegate { }));
        Components[type] = new();
        return this;
    }

    /// <summary>Adds a possible implementation, like `TopticaComponent`</summary>
    public ExperimentContainer AddComponent<T>() where T : ExperimentComponentClass
    {
        var typeToAdd = typeof(T);
        if (Components.Values.Any(types => types.Contains(typeToAdd)))
        {
            throw new ArgumentException($"Component {typeToAdd} has already been added");
        }
        var hasBeenAdded = false;
        foreach (var (componentClass, components) in Components)
        {
            if (typeToAdd.IsAssignableTo(componentClass))
            {
                components.Add(typeToAdd);
                hasBeenAdded = true;
            }
        }
        if (!hasBeenAdded)
        {
            throw new ArgumentException($"Component {typeToAdd} does not match any registered component classes");
        }
        return this;
    }

    public IEnumerable<(Type Class, string Id)> GetComponentClasses() => ComponentClasses.Select(c => (c.Class, c.Id));

    public IEnumerable<(Type type, string Name, string DisplayName)> GetComponents(Type componentClass) =>
        Components[componentClass].Select(type => (type, type.Name, ExperimentComponentClass.GetName(type)));


    /// <summary>Gets the currently active implementation for the component type `T`</summary>
    public T? GetActiveComponent<T>(string? classId = null) where T : ExperimentComponentClass => (T?)GetActiveComponent(typeof(T), classId);
    public ExperimentComponentClass? GetActiveComponent(Type componentClass, string? classId = null)
    {
        var entry = ComponentClasses.FirstOrDefault(c => c.Class == componentClass && (classId == null || c.Id == classId));
        if (entry == null)
        {
            throw new ArgumentException($"Unknown component class {componentClass}{(classId != null ? $" ({classId})" : "")}");
        }
        return entry.ActiveComponent;
    }

    public void InitFrom(Func<(Type Class, string Id), (string? componentName, object? settings)?> cb)
    {
        foreach (var entry in ComponentClasses)
        {
            var result = cb((entry.Class, entry.Id));
            if (result is null || result.Value.componentName == null)
            {
                continue;
            }
            try
            {
                InitComponent(entry.Class, entry.Id, result.Value.componentName, result.Value.settings);
            }
            catch { }
        }
    }

    public void InitComponent(Type componentClass, string? classId, string componentName, object? settings = null)
    {
        var entry = ComponentClasses.FirstOrDefault(c => c.Class == componentClass && (classId == null || c.Id == classId));
        if (entry == null)
        {
            throw new ArgumentException($"Unknown component class {componentClass}{(classId != null ? $" ({classId})" : "")}");
        }
        classId = entry.Id;
        var typeOfComponent = GetComponentTypeFromString(componentName) ?? throw new ArgumentException($"Component {componentName} not found");

        var settingsType = ExperimentComponentClass.GetSettingsType(typeOfComponent);
        if (settingsType != null && !(settingsType.IsAssignableFrom(settings?.GetType())))
        {
            throw new ArgumentException($"Settings type {settings?.GetType().Name} is not assignable to {settingsType}");
        }

        entry.ActiveComponent?.Dispose();
        var newInstance = (settingsType == null ? Activator.CreateInstance(typeOfComponent) : Activator.CreateInstance(typeOfComponent, settings))
            as ExperimentComponentClass ?? throw new ArgumentException($"Component {componentName} couldn't be initialized");
        entry.ActiveComponent = newInstance;

        entry.ChangeHandler.Invoke(newInstance);
        ComponentChanged.Invoke(componentClass, classId, newInstance);
    }

    public void AddComponentChangeHandler<T>(Action<T?> cb) where T : ExperimentComponentClass => AddComponentChangeHandler<T>(null, cb);
    public void AddComponentChangeHandler<T>(string? classId, Action<T?> cb) where T : ExperimentComponentClass
    {
        var type = typeof(T);
        var entry = ComponentClasses.FirstOrDefault(c => c.Class == type && (classId == null || c.Id == classId));
        if (entry == null)
        {
            throw new ArgumentException($"Unknown component class {type.Name}{(classId != null ? $" ({classId})" : "")}");
        }
        entry.ChangeHandler += (component) => cb((T?)component);
    }

    public Type? GetComponentTypeFromString(string componentName)
    {
        return Components.SelectMany(entry => entry.Value).FirstOrDefault(type => type.Name == componentName);
    }

    public void Dispose()
    {
        foreach (var entry in ComponentClasses)
        {
            entry.ActiveComponent?.Dispose();
            entry.ActiveComponent = null;
        }
    }
}
