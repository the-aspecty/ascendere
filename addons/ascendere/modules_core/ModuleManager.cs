using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Ascendere.Modular;

public partial class ModuleManager : Node
{
    private static ModuleManager _instance;
    public static ModuleManager Instance => _instance;

    private readonly Dictionary<Type, IModule> _modules = new();
    private readonly List<Assembly> _scannedAssemblies = new();

    //private ModuleContext _moduleContext;

    public override void _EnterTree()
    {
        if (_instance == null)
        {
            _instance = this;
            //_moduleContext = new ModuleContext(this, _externalModuleManifests);
            ScanForModules();
        }
        else
        {
            QueueFree();
        }
    }

    public T GetModule<T>()
        where T : class, IModule
    {
        return _modules.TryGetValue(typeof(T), out var module) ? (T)module : null;
    }

    private void ScanForModules()
    {
        // First scan for traditional attribute-based modules
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (_scannedAssemblies.Contains(assembly))
                continue;

            try
            {
                ScanAssemblyForModules(assembly);
                _scannedAssemblies.Add(assembly);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error scanning assembly {assembly.FullName}: {e.Message}");
            }
        }

        InitializeModules();
    }

    private void ScanAssemblyForModules(Assembly assembly)
    {
        var moduleTypes = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<ModuleAttribute>() != null)
            .OrderBy(t => t.GetCustomAttribute<ModuleAttribute>().LoadOrder);

        foreach (var moduleType in moduleTypes)
        {
            RegisterModule(moduleType);
        }
    }

    private void RegisterModule(Type moduleType)
    {
        var attr = moduleType.GetCustomAttribute<ModuleAttribute>();
        if (attr?.AutoLoad != true)
            return;

        try
        {
            var module = Activator.CreateInstance(moduleType) as IModule;
            if (module == null)
                return;

            if (string.IsNullOrEmpty(module.Name) && !string.IsNullOrEmpty(attr.Name))
            {
                var nameProperty = moduleType.GetProperty("Name");
                if (nameProperty != null)
                {
                    var propType = nameProperty.PropertyType;
                    if (propType == typeof(StringName))
                    {
                        nameProperty.SetValue(module, new StringName(attr.Name));
                    }
                    else
                    {
                        nameProperty.SetValue(module, attr.Name);
                    }
                }
            }

            _modules[moduleType] = module;

            if (module is Node nodeModule)
            {
                GD.Print($"Creating module {moduleType.Name}");
                //ServiceLocator.RegisterServiceInferType(module);
                nodeModule.Name = new StringName(module.Name);
                AddChild(nodeModule, true);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error creating module {moduleType.Name}: {e.Message}");
        }
    }

    private void InitializeModules()
    {
        // Initialize main modules first
        foreach (var module in _modules.Values)
        {
            try
            {
                module?.Initialize();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error initializing module {module.Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Get all registered modules (for ModuleContext and Editor)
    /// </summary>
    public IReadOnlyDictionary<Type, IModule> GetAllModules()
    {
        return _modules.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public override void _ExitTree()
    {
        foreach (var module in _modules.Values)
        {
            try
            {
                module.Cleanup();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error cleaning up module {module.Name}: {e.Message}");
            }
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }
}
