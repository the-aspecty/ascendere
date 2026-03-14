using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ascendere.EditorRuntime;
using Godot;

// === CORE SERVICE LOCATOR ===

/// <summary>
/// Advanced Service Locator with DI, decorators, middleware, and named services
/// </summary>
public partial class ServiceLocator : Node
{
    private static ServiceLocator _instance;
    private readonly Dictionary<ServiceKey, ServiceDescriptor> _services = new();
    private readonly Dictionary<ServiceKey, object> _singletons = new();
    private readonly Dictionary<string, ServiceScope> _scopes = new();
    private readonly List<IDisposable> _disposables = new();
    private readonly List<IServiceMiddleware> _middlewares = new();
    private readonly Dictionary<Type, List<DecoratorInfo>> _decorators = new();

    public static event Action<Type, string> OnServiceRegistered;
    public static event Action<Type, string> OnServiceResolved;

    public override void _EnterTree()
    {
        if (_instance != null)
        {
            QueueFree();
            return;
        }
        _instance = this;
        AutoRegisterServices();
        AutoRegisterDecorators();
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            DisposeServices();
            _instance = null;
            _services.Clear();
            _singletons.Clear();
            _scopes.Clear();
            _middlewares.Clear();
            _decorators.Clear();
        }
    }

    /// <summary>
    /// Automatically discover and register services with [Service] attribute
    /// </summary>
    private void AutoRegisterServices()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var serviceTypes = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes<ServiceAttribute>().Any())
            .OrderByDescending(t => t.GetCustomAttributes<ServiceAttribute>().Max(a => a.Priority));

        // Collect services to eagerly resolve after all registrations
        var eagerServices = new List<(Type serviceType, string name)>();

        foreach (var type in serviceTypes)
        {
            var attributes = type.GetCustomAttributes<ServiceAttribute>();

            foreach (var attr in attributes)
            {
                var serviceType = attr.InterfaceType ?? type;
                var key = new ServiceKey(serviceType, attr.Name);

                var descriptor = new ServiceDescriptor(type, attr.Lifetime, attr.Lazy);
                _services[key] = descriptor;

                OnServiceRegistered?.Invoke(serviceType, attr.Name);

                var nameInfo = string.IsNullOrEmpty(attr.Name) ? "" : $" (Name: {attr.Name})";
                GD.Print(
                    $"[ServiceLocator] Registered: {serviceType.Name} -> {type.Name}{nameInfo} ({attr.Lifetime}, Priority: {attr.Priority}, Lazy: {attr.Lazy})"
                );
                //call defered
                CallDeferred(nameof(ReportService), serviceType.Name);

                // Queue non-lazy singletons for eager resolution
                if (!attr.Lazy && attr.Lifetime == ServiceLifetime.Singleton)
                {
                    eagerServices.Add((serviceType, attr.Name));
                }
            }
        }

        // Eagerly resolve non-lazy singletons (this will add Node services to tree)
        foreach (var (serviceType, name) in eagerServices)
        {
            Get(serviceType, name);
        }
    }

    /// <summary>
    /// Automatically discover and register decorators
    /// </summary>
    private void AutoRegisterDecorators()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var decoratorTypes = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<DecoratorAttribute>() != null)
            .OrderBy(t => t.GetCustomAttribute<DecoratorAttribute>().Order);

        foreach (var type in decoratorTypes)
        {
            var attr = type.GetCustomAttribute<DecoratorAttribute>();

            if (!_decorators.ContainsKey(attr.ServiceType))
                _decorators[attr.ServiceType] = new List<DecoratorInfo>();

            _decorators[attr.ServiceType].Add(new DecoratorInfo(type, attr.Order));

            GD.Print(
                $"[ServiceLocator] Registered decorator: {type.Name} for {attr.ServiceType.Name} (Order: {attr.Order})"
            );
        }
    }

    // === MIDDLEWARE ===

    /// <summary>
    /// Add middleware to the service resolution pipeline
    /// </summary>
    public static void UseMiddleware(IServiceMiddleware middleware)
    {
        if (_instance == null)
            return;
        _instance._middlewares.Add(middleware);
        GD.Print($"[ServiceLocator] Added middleware: {middleware.GetType().Name}");
    }

    /// <summary>
    /// Add middleware using a factory function
    /// </summary>
    public static void UseMiddleware(
        Func<ServiceContext, Func<Task<object>>, Task<object>> middleware
    )
    {
        UseMiddleware(new DelegateMiddleware(middleware));
    }

    private async Task<object> ExecuteMiddlewarePipeline(
        ServiceContext context,
        Func<Task<object>> finalHandler
    )
    {
        if (_middlewares.Count == 0)
            return await finalHandler();

        Func<Task<object>> pipeline = finalHandler;

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var next = pipeline;
            pipeline = () => middleware.InvokeAsync(context, next);
        }

        return await pipeline();
    }

    // === REGISTRATION ===

    public static void Register<TInterface, TImplementation>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        bool lazy = false,
        string name = null
    )
        where TImplementation : class, TInterface
    {
        Register(typeof(TInterface), typeof(TImplementation), lifetime, lazy, name);
    }

    public static void Register<T>(T instance, string name = null)
        where T : class
    {
        if (_instance == null)
            return;

        var type = typeof(T);
        var key = new ServiceKey(type, name);

        _instance._singletons[key] = instance;
        _instance._services[key] = new ServiceDescriptor(type, ServiceLifetime.Singleton, false);

        // Add Node services to scene tree so they receive lifecycle callbacks
        if (instance is Node node && !node.IsInsideTree())
        {
            // This is a singleton instance - keep name stable (no suffix)
            SetServiceNodeName(node, type, name, instance.GetType(), addSuffix: false);
            _instance.CallDeferred(Node.MethodName.AddChild, node);
        }

        if (instance is IDisposable disposable)
            _instance._disposables.Add(disposable);

        OnServiceRegistered?.Invoke(type, name);

        var nameInfo = string.IsNullOrEmpty(name) ? "" : $" (Name: {name})";
        GD.Print($"[ServiceLocator] Registered instance: {type.Name}{nameInfo}");
    }

    public static void Register(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        bool lazy = false,
        string name = null
    )
    {
        if (_instance == null)
            return;

        var key = new ServiceKey(serviceType, name);
        _instance._services[key] = new ServiceDescriptor(implementationType, lifetime, lazy);
        OnServiceRegistered?.Invoke(serviceType, name);
    }

    public static void RegisterFactory<T>(
        Func<T> factory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        string name = null
    )
        where T : class
    {
        if (_instance == null)
            return;

        var type = typeof(T);
        var key = new ServiceKey(type, name);

        _instance._services[key] = new ServiceDescriptor(() => factory(), lifetime, false);
        OnServiceRegistered?.Invoke(type, name);

        var nameInfo = string.IsNullOrEmpty(name) ? "" : $" (Name: {name})";
        GD.Print($"[ServiceLocator] Registered factory: {type.Name}{nameInfo}");
    }

    // === DECORATOR REGISTRATION ===

    public static void RegisterDecorator<TService, TDecorator>(int order = 0)
        where TService : class
        where TDecorator : IServiceDecorator<TService>
    {
        if (_instance == null)
            return;

        var serviceType = typeof(TService);
        if (!_instance._decorators.ContainsKey(serviceType))
            _instance._decorators[serviceType] = new List<DecoratorInfo>();

        _instance._decorators[serviceType].Add(new DecoratorInfo(typeof(TDecorator), order));
        _instance._decorators[serviceType] = _instance
            ._decorators[serviceType]
            .OrderBy(d => d.Order)
            .ToList();

        GD.Print(
            $"[ServiceLocator] Registered decorator: {typeof(TDecorator).Name} for {serviceType.Name}"
        );
    }

    // === RESOLUTION ===

    public static T Get<T>(string name = null)
        where T : class
    {
        return Get(typeof(T), name) as T;
    }

    public static object Get(Type serviceType, string name = null)
    {
        if (_instance == null)
        {
            GD.PrintErr("[ServiceLocator] Not initialized!");
            return null;
        }

        var key = new ServiceKey(serviceType, name);

        if (!_instance._services.TryGetValue(key, out var descriptor))
        {
            var nameInfo = string.IsNullOrEmpty(name) ? "" : $" with name '{name}'";
            GD.PrintErr($"[ServiceLocator] Service {serviceType.Name}{nameInfo} not registered!");
            return null;
        }

        // Execute middleware pipeline
        var context = new ServiceContext(serviceType, name);
        var task = _instance.ExecuteMiddlewarePipeline(
            context,
            async () =>
            {
                return await Task.FromResult(ResolveService(key, descriptor, serviceType));
            }
        );

        var result = task.GetAwaiter().GetResult();
        OnServiceResolved?.Invoke(serviceType, name);

        return result;
    }

    private static object ResolveService(
        ServiceKey key,
        ServiceDescriptor descriptor,
        Type serviceType
    )
    {
        object instance = null;

        // Check for singleton
        if (descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            if (_instance._singletons.TryGetValue(key, out var singleton))
                return singleton;
        }

        // Create instance
        if (descriptor.Factory != null)
        {
            instance = descriptor.Factory();
        }
        else
        {
            instance = CreateInstance(descriptor.ImplementationType);
        }

        // Apply decorators
        if (instance != null && _instance._decorators.TryGetValue(serviceType, out var decorators))
        {
            instance = ApplyDecorators(instance, serviceType, decorators);
        }

        // Cache singleton
        if (descriptor.Lifetime == ServiceLifetime.Singleton && instance != null)
        {
            _instance._singletons[key] = instance;

            // Add Node services to scene tree so they receive lifecycle callbacks
            if (instance is Node node && !node.IsInsideTree())
            {
                // Cached singletons should have stable names (no suffix)
                SetServiceNodeName(
                    node,
                    serviceType,
                    null,
                    descriptor.ImplementationType ?? instance.GetType(),
                    addSuffix: false
                );
                _instance.CallDeferred(Node.MethodName.AddChild, node);
            }

            if (instance is IDisposable disposable)
                _instance._disposables.Add(disposable);
        }

        return instance;
    }

    private static object ApplyDecorators(
        object instance,
        Type serviceType,
        List<DecoratorInfo> decorators
    )
    {
        var current = instance;

        foreach (var decoratorInfo in decorators)
        {
            try
            {
                var decorator = Activator.CreateInstance(decoratorInfo.Type);
                var decorateMethod = decoratorInfo.Type.GetMethod("Decorate");

                if (decorateMethod != null)
                {
                    current = decorateMethod.Invoke(decorator, new[] { current });
                    GD.Print(
                        $"[ServiceLocator] Applied decorator {decoratorInfo.Type.Name} to {serviceType.Name}"
                    );
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(
                    $"[ServiceLocator] Failed to apply decorator {decoratorInfo.Type.Name}: {e.Message}"
                );
            }
        }

        return current;
    }

    public static T GetOrDefault<T>(string name = null)
        where T : class
    {
        return Has<T>(name) ? Get<T>(name) : null;
    }

    public static bool TryGet<T>(out T service, string name = null)
        where T : class
    {
        service = GetOrDefault<T>(name);
        return service != null;
    }

    public static IEnumerable<T> GetAll<T>()
        where T : class
    {
        if (_instance == null)
            yield break;

        var targetType = typeof(T);
        foreach (var kvp in _instance._services)
        {
            if (targetType.IsAssignableFrom(kvp.Key.Type))
            {
                var service = Get(kvp.Key.Type, kvp.Key.Name) as T;
                if (service != null)
                    yield return service;
            }
        }
    }

    public static Dictionary<string, T> GetAllNamed<T>()
        where T : class
    {
        var result = new Dictionary<string, T>();
        if (_instance == null)
            return result;

        var targetType = typeof(T);
        foreach (var kvp in _instance._services)
        {
            if (kvp.Key.Type == targetType && !string.IsNullOrEmpty(kvp.Key.Name))
            {
                var service = Get<T>(kvp.Key.Name);
                if (service != null)
                    result[kvp.Key.Name] = service;
            }
        }

        return result;
    }

    // === SCOPES ===

    public static ServiceScope CreateScope(string name = null)
    {
        if (_instance == null)
            return null;

        name ??= Guid.NewGuid().ToString();
        var scope = new ServiceScope(name);
        _instance._scopes[name] = scope;

        GD.Print($"[ServiceLocator] Created scope: {name}");
        return scope;
    }

    public static void DisposeScope(string name)
    {
        if (_instance == null)
            return;

        if (_instance._scopes.TryGetValue(name, out var scope))
        {
            scope.Dispose();
            _instance._scopes.Remove(name);
            GD.Print($"[ServiceLocator] Disposed scope: {name}");
        }
    }

    // === INSTANCE CREATION ===

    private static object CreateInstance(Type type)
    {
        var constructor = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (constructor == null)
        {
            GD.PrintErr($"[ServiceLocator] No constructor for {type.Name}");
            return null;
        }

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = Get(parameters[i].ParameterType);
        }

        var instance = Activator.CreateInstance(type, args);
        InjectMembers(instance);
        CallPostInject(instance);

        return instance;
    }

    public static void InjectMembers(object target)
    {
        if (target == null)
            return;

        var type = target.GetType();

        // Inject properties
        var properties = type.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            )
            .Where(p => p.GetCustomAttribute<InjectAttribute>() != null);

        foreach (var prop in properties)
        {
            if (prop.CanWrite)
            {
                var attr = prop.GetCustomAttribute<InjectAttribute>();
                var service = GetOrDefault(prop.PropertyType, attr.Name);

                if (service != null)
                    prop.SetValue(target, service);
                else if (!attr.Optional)
                {
                    var nameInfo = string.IsNullOrEmpty(attr.Name)
                        ? ""
                        : $" with name '{attr.Name}'";
                    GD.PrintErr(
                        $"[ServiceLocator] Required service {prop.PropertyType.Name}{nameInfo} not found for {type.Name}"
                    );
                }
            }
        }

        // Inject fields
        var fields = type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            )
            .Where(f => f.GetCustomAttribute<InjectAttribute>() != null);

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<InjectAttribute>();
            var service = GetOrDefault(field.FieldType, attr.Name);

            if (service != null)
                field.SetValue(target, service);
            else if (!attr.Optional)
            {
                var nameInfo = string.IsNullOrEmpty(attr.Name) ? "" : $" with name '{attr.Name}'";
                GD.PrintErr(
                    $"[ServiceLocator] Required service {field.FieldType.Name}{nameInfo} not found for {type.Name}"
                );
            }
        }
    }

    private static object GetOrDefault(Type type, string name = null)
    {
        return Has(type, name) ? Get(type, name) : null;
    }

    private static void CallPostInject(object instance)
    {
        var methods = instance
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<PostInjectAttribute>() != null);

        foreach (var method in methods)
        {
            method.Invoke(instance, null);
        }
    }

    /// <summary>
    /// Assigns a descriptive name to Node-based services when they're added to the scene tree.
    /// Preserves custom names (does not overwrite if user provided a non-default name).
    /// </summary>
    private static void SetServiceNodeName(
        Node node,
        Type serviceType,
        string name,
        Type implementationType,
        bool addSuffix = true
    )
    {
        if (node == null)
            return;

        // If the node already has a custom name (different from the default type name), keep it
        var defaultName = node.GetType().Name;
        if (!string.IsNullOrEmpty(node.Name) && node.Name != defaultName)
            return;

        var baseName = implementationType?.Name ?? serviceType?.Name ?? node.GetType().Name;
        var annotated = string.IsNullOrEmpty(name) ? baseName : $"{baseName} ({name})";

        if (addSuffix)
        {
            // Append a short unique suffix to help distinguish multiple instances
            var suffix = Math.Abs(node.GetHashCode()).ToString("X8");
            node.Name = $"{annotated}_{suffix}";
        }
        else
        {
            node.Name = annotated;
        }
    }

    // === INITIALIZATION ===

    public static async Task InitializeServicesAsync()
    {
        if (_instance == null)
            return;

        GD.Print("[ServiceLocator] Initializing services...");

        foreach (var kvp in _instance._services)
        {
            var service = Get(kvp.Key.Type, kvp.Key.Name);
            if (service == null)
                continue;

            var initMethod = service
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetCustomAttribute<InitializeAttribute>() != null);

            if (initMethod != null)
            {
                var nameInfo = string.IsNullOrEmpty(kvp.Key.Name) ? "" : $" ({kvp.Key.Name})";
                GD.Print($"[ServiceLocator] Initializing {kvp.Key.Type.Name}{nameInfo}...");
                var result = initMethod.Invoke(service, null);

                if (result is Task task)
                    await task;
            }
        }

        GD.Print("[ServiceLocator] All services initialized!");
    }

    // === UTILITIES ===

    public static bool Has<T>(string name = null) => Has(typeof(T), name);

    public static bool Has(Type type, string name = null)
    {
        if (_instance == null)
            return false;
        var key = new ServiceKey(type, name);
        return _instance._services.ContainsKey(key);
    }

    public static void Unregister<T>(string name = null)
    {
        if (_instance == null)
            return;

        var type = typeof(T);
        var key = new ServiceKey(type, name);

        if (_instance._singletons.TryGetValue(key, out var instance))
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
                _instance._disposables.Remove(disposable);
            }

            // Remove Node services from scene tree
            if (instance is Node node && IsInstanceValid(node) && node.IsInsideTree())
                node.QueueFree();

            _instance._singletons.Remove(key);
        }

        _instance._services.Remove(key);
    }

    public static void Reset()
    {
        if (_instance == null)
            return;

        DisposeServices();
        _instance._services.Clear();
        _instance._singletons.Clear();
        _instance._scopes.Clear();
        _instance._middlewares.Clear();
        _instance.AutoRegisterServices();
        _instance.AutoRegisterDecorators();

        GD.Print("[ServiceLocator] Reset complete!");
    }

    private static void DisposeServices()
    {
        if (_instance == null)
            return;

        foreach (var disposable in _instance._disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                GD.PrintErr($"[ServiceLocator] Dispose error: {e.Message}");
            }
        }
        _instance._disposables.Clear();

        // Clean up Node services from scene tree
        foreach (var singleton in _instance._singletons.Values)
        {
            if (singleton is Node node && IsInstanceValid(node) && node.IsInsideTree())
                node.QueueFree();
        }

        foreach (var scope in _instance._scopes.Values)
        {
            scope.Dispose();
        }
    }

    // === DEBUGGING ===

    public static void PrintServices()
    {
        if (_instance == null)
            return;

        GD.Print("=== Registered Services ===");
        foreach (var kvp in _instance._services)
        {
            var isSingleton = _instance._singletons.ContainsKey(kvp.Key) ? " [Cached]" : "";
            var nameInfo = string.IsNullOrEmpty(kvp.Key.Name) ? "" : $" (Name: {kvp.Key.Name})";
            GD.Print(
                $"  {kvp.Key.Type.Name}{nameInfo} -> {kvp.Value.ImplementationType?.Name ?? "Factory"} ({kvp.Value.Lifetime}){isSingleton}"
            );
        }

        if (_instance._decorators.Count > 0)
        {
            GD.Print("\n=== Registered Decorators ===");
            foreach (var kvp in _instance._decorators)
            {
                GD.Print($"  {kvp.Key.Name}:");
                foreach (var decorator in kvp.Value)
                {
                    GD.Print($"    - {decorator.Type.Name} (Order: {decorator.Order})");
                }
            }
        }

        if (_instance._middlewares.Count > 0)
        {
            GD.Print("\n=== Registered Middleware ===");
            foreach (var middleware in _instance._middlewares)
            {
                GD.Print($"  - {middleware.GetType().Name}");
            }
        }
    }

    // === DEPENDENCY GRAPH ===

    public static DependencyGraph BuildDependencyGraph()
    {
        if (_instance == null)
            return null;

        var graph = new DependencyGraph();

        foreach (var kvp in _instance._services)
        {
            var serviceKey = kvp.Key;
            var descriptor = kvp.Value;

            if (descriptor.ImplementationType == null)
                continue;

            var node = new ServiceNode
            {
                ServiceType = serviceKey.Type,
                ImplementationType = descriptor.ImplementationType,
                ServiceName = serviceKey.Name,
                Lifetime = descriptor.Lifetime,
            };

            // Analyze constructor dependencies
            var constructor = descriptor
                .ImplementationType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor != null)
            {
                foreach (var param in constructor.GetParameters())
                {
                    node.Dependencies.Add(
                        new ServiceDependency
                        {
                            DependencyType = param.ParameterType,
                            IsRequired = true,
                            InjectionPoint = "Constructor",
                        }
                    );
                }
            }

            // Analyze property/field dependencies
            var properties = descriptor
                .ImplementationType.GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                )
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null);

            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<InjectAttribute>();
                node.Dependencies.Add(
                    new ServiceDependency
                    {
                        DependencyType = prop.PropertyType,
                        DependencyName = attr.Name,
                        IsRequired = !attr.Optional,
                        InjectionPoint = $"Property: {prop.Name}",
                    }
                );
            }

            var fields = descriptor
                .ImplementationType.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                )
                .Where(f => f.GetCustomAttribute<InjectAttribute>() != null);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<InjectAttribute>();
                node.Dependencies.Add(
                    new ServiceDependency
                    {
                        DependencyType = field.FieldType,
                        DependencyName = attr.Name,
                        IsRequired = !attr.Optional,
                        InjectionPoint = $"Field: {field.Name}",
                    }
                );
            }

            graph.AddNode(node);
        }

        return graph;
    }

    public static void PrintDependencyGraph()
    {
        var graph = BuildDependencyGraph();
        if (graph == null)
            return;

        GD.Print("\n=== Dependency Graph ===");

        foreach (var node in graph.Nodes)
        {
            var nameInfo = string.IsNullOrEmpty(node.ServiceName) ? "" : $" ({node.ServiceName})";
            GD.Print($"\n[{node.ServiceType.Name}{nameInfo}]");
            GD.Print($"  Implementation: {node.ImplementationType.Name}");
            GD.Print($"  Lifetime: {node.Lifetime}");

            if (node.Dependencies.Count > 0)
            {
                GD.Print("  Dependencies:");
                foreach (var dep in node.Dependencies)
                {
                    var depNameInfo = string.IsNullOrEmpty(dep.DependencyName)
                        ? ""
                        : $" ({dep.DependencyName})";
                    var required = dep.IsRequired ? "[Required]" : "[Optional]";
                    GD.Print(
                        $"    → {dep.DependencyType.Name}{depNameInfo} {required} via {dep.InjectionPoint}"
                    );
                }
            }
            else
            {
                GD.Print("  Dependencies: None");
            }
        }

        // Detect circular dependencies
        var cycles = graph.DetectCircularDependencies();
        if (cycles.Count > 0)
        {
            GD.Print("\n⚠️  CIRCULAR DEPENDENCIES DETECTED:");
            foreach (var cycle in cycles)
            {
                GD.Print($"  → {string.Join(" → ", cycle.Select(t => t.Name))}");
            }
        }
        else
        {
            GD.Print("\n✓ No circular dependencies detected");
        }

        // Show initialization order
        var order = graph.GetInitializationOrder();
        if (order.Count > 0)
        {
            GD.Print("\n=== Recommended Initialization Order ===");
            for (int i = 0; i < order.Count; i++)
            {
                var nameInfo = string.IsNullOrEmpty(order[i].ServiceName)
                    ? ""
                    : $" ({order[i].ServiceName})";
                GD.Print($"  {i + 1}. {order[i].ServiceType.Name}{nameInfo}");
            }
        }
    }

    public static string ExportDependencyGraphMermaid()
    {
        var graph = BuildDependencyGraph();
        if (graph == null)
            return "";

        return graph.ToMermaid();
    }

    public static string ExportDependencyGraphDot()
    {
        var graph = BuildDependencyGraph();
        if (graph == null)
            return "";

        return graph.ToDot();
    }

    public static void SaveDependencyGraphToFile(
        string path,
        GraphFormat format = GraphFormat.Mermaid
    )
    {
        string content = format switch
        {
            GraphFormat.Mermaid => ExportDependencyGraphMermaid(),
            GraphFormat.Dot => ExportDependencyGraphDot(),
            _ => "",
        };

        if (!string.IsNullOrEmpty(content))
        {
            // Ensure we're working with a valid path
            if (string.IsNullOrEmpty(path))
            {
                GD.PrintErr("[ServiceLocator] Invalid path provided");
                return;
            }

            // Get file extension - the FileAccess.Open handles res:// paths automatically
            var extension = path.GetExtension().ToLower();

            // Wrap mermaid content with markdown code fence if saving to .md file
            if (format == GraphFormat.Mermaid && extension == "md")
            {
                content = $"```mermaid\n{content}\n```";
                GD.Print($"[ServiceLocator] Wrapped content with mermaid code fence");
            }

            // Ensure directory exists
            var directory = path.GetBaseDir();
            if (!DirAccess.DirExistsAbsolute(directory))
            {
                var error = DirAccess.MakeDirAbsolute(directory);
                if (error != Error.Ok)
                {
                    GD.PrintErr($"[ServiceLocator] Failed to create directory: {directory}");
                    return;
                }
            }

            // Write file and ensure proper cleanup
            using var fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (fileAccess != null)
            {
                fileAccess.StoreString(content);
                GD.Print($"[ServiceLocator] File written successfully");
            }
            else
            {
                GD.PrintErr($"[ServiceLocator] Failed to write dependency graph to: {path}");
                return;
            }

            GD.Print($"[ServiceLocator] Dependency graph saved to: {path}");
        }
    }

    // === NESTED CLASSES ===

    private struct ServiceKey : IEquatable<ServiceKey>
    {
        public Type Type { get; }
        public string Name { get; }

        public ServiceKey(Type type, string name)
        {
            Type = type;
            Name = name;
        }

        public bool Equals(ServiceKey other)
        {
            return Type == other.Type && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Name);
        }
    }

    private class ServiceDescriptor
    {
        public Type ImplementationType { get; }
        public ServiceLifetime Lifetime { get; }
        public bool IsLazy { get; }
        public Func<object> Factory { get; }

        public ServiceDescriptor(Type implementationType, ServiceLifetime lifetime, bool isLazy)
        {
            ImplementationType = implementationType;
            Lifetime = lifetime;
            IsLazy = isLazy;
        }

        public ServiceDescriptor(Func<object> factory, ServiceLifetime lifetime, bool isLazy)
        {
            Factory = factory;
            Lifetime = lifetime;
            IsLazy = isLazy;
        }
    }

    private class DecoratorInfo
    {
        public Type Type { get; }
        public int Order { get; }

        public DecoratorInfo(Type type, int order)
        {
            Type = type;
            Order = order;
        }
    }

    private class DelegateMiddleware : IServiceMiddleware
    {
        private readonly Func<ServiceContext, Func<Task<object>>, Task<object>> _middleware;

        public DelegateMiddleware(Func<ServiceContext, Func<Task<object>>, Task<object>> middleware)
        {
            _middleware = middleware;
        }

        public Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next)
        {
            return _middleware(context, next);
        }
    }

    public class ServiceScope : IDisposable
    {
        public string Name { get; }
        private readonly Dictionary<ServiceKey, object> _scopedInstances = new();
        private bool _disposed;

        public ServiceScope(string name)
        {
            Name = name;
        }

        public T Get<T>(string name = null)
            where T : class
        {
            if (_disposed)
                return null;

            var type = typeof(T);
            var key = new ServiceKey(type, name);

            if (_scopedInstances.TryGetValue(key, out var instance))
                return instance as T;

            var service = ServiceLocator.Get<T>(name);
            if (service != null)
                _scopedInstances[key] = service;

            return service;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var instance in _scopedInstances.Values)
            {
                if (instance is IDisposable disposable)
                    disposable.Dispose();
            }

            _scopedInstances.Clear();
            _disposed = true;
        }
    }

    private void ReportService(string serviceName)
    {
        if (!string.IsNullOrEmpty(serviceName))
        {
            RuntimeBridge.Instance?.Send(new TextMessage(serviceName));
        }
    }
}
