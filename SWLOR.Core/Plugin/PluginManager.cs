using System.Reflection;
using SWLOR.Core.Plugin.Paket;

namespace SWLOR.Core.Plugin
{
    /// <summary>
    /// Loads all available plugins and their types for service initialisation.
    /// </summary>
    public sealed class PluginManager
    {
        private const int PluginUnloadAttempts = 10;
        private const int PluginUnloadSleepMs = 5000;

        private readonly ApplicationSettings _appSettings = ApplicationSettings.Get();
        private readonly HashSet<Assembly> _loadedAssemblies = new();
        private readonly List<Plugin> _plugins = new();

        internal IReadOnlyCollection<Type> LoadedTypes { get; private set; } = null!;

        internal IReadOnlyCollection<string> ResourcePaths { get; private set; } = null!;

        /// <summary>
        /// Gets the install directory of the specified plugin.
        /// </summary>
        /// <param name="pluginAssembly">The assembly of the plugin, e.g. typeof(MyService).Assembly</param>
        /// <returns>The install directory for the specified plugin.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified assembly is not a plugin.</exception>
        public string? GetPluginDirectory(Assembly pluginAssembly)
        {
            if (IsPluginAssembly(pluginAssembly))
            {
                return Path.GetDirectoryName(pluginAssembly.Location);
            }

            throw new ArgumentException("Specified assembly is not a loaded plugin assembly.", nameof(pluginAssembly));
        }

        /// <summary>
        /// Gets if the specified assembly is the primary assembly for a plugin.
        /// </summary>
        /// <param name="assembly">The assembly to query.</param>
        /// <returns>True if the assembly is a plugin, otherwise false.</returns>
        public bool IsPluginAssembly(Assembly assembly)
        {
            return _plugins.Any(plugin => plugin.Assembly == assembly);
        }

        public bool IsPluginLoaded(string pluginName)
        {
            return _plugins.Any(plugin => plugin.Name.Name == pluginName);
        }

        public Assembly? ResolveDependency(string pluginName, AssemblyName dependencyName)
        {
            var assembly = ResolveDependencyFromSwlor(pluginName, dependencyName);
            if (assembly == null)
            {
                assembly = ResolveDependencyFromPlugins(pluginName, dependencyName);
            }

            return assembly;
        }


        public void Load()
        {
            LoadCore();
            BootstrapPlugins();
            LoadPlugins();

            LoadedTypes = GetLoadedTypes();
            ResourcePaths = GetResourcePaths();
        }

        public void Unload()
        {
            _loadedAssemblies.Clear();
            LoadedTypes = null!;
            ResourcePaths = null!;

            Console.WriteLine($"Unloading plugins...");
            var pendingUnloads = new Dictionary<WeakReference, string>();
            foreach (var plugin in _plugins)
            {
                Console.WriteLine($"Unloading DotNET plugin {plugin.Name.Name} - {plugin.Path}");
                pendingUnloads.Add(plugin.Unload(), plugin.Name.Name!);
            }

            _plugins.Clear();
            _plugins.TrimExcess();

            if (_appSettings.IsHotReloadEnabled)
            {
                for (var unloadAttempt = 1; !IsUnloadComplete(pendingUnloads, unloadAttempt); unloadAttempt++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (unloadAttempt > PluginUnloadAttempts)
                    {
                        Thread.Sleep(PluginUnloadSleepMs);
                    }
                }
            }
        }

        private void BootstrapPlugins()
        {
            var pluginSources = new List<IPluginSource> 
            {
                new PaketPluginSource(this),
                new LocalPluginSource(this, _appSettings.PluginDirectory),
            };

            foreach (var pluginSource in pluginSources)
            {
                _plugins.AddRange(pluginSource.Bootstrap());
            }
        }

        private IReadOnlyCollection<Type> GetLoadedTypes()
        {
            var loadedTypes = new List<Type>();
            foreach (var assembly in _loadedAssemblies)
            {
                loadedTypes.AddRange(GetTypesFromAssembly(assembly));
            }

            return loadedTypes;
        }

        private IReadOnlyCollection<string> GetResourcePaths()
        {
            var resourcePaths = new List<string>();
            foreach (var plugin in _plugins)
            {
                if (plugin.HasResourceDirectory)
                {
                    resourcePaths.Add(plugin.ResourcePath!);
                }
            }

            return resourcePaths;
        }

        private IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            IEnumerable<Type> assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                var pluginInfoAttribute = assembly.GetCustomAttribute<PluginInfoAttribute>();
                if (pluginInfoAttribute?.OptionalDependencies == null)
                {
                    throw;
                }

                foreach (var exception in e.LoaderExceptions)
                {
                    if (exception is FileNotFoundException fileNotFoundException)
                    {
                        var assemblyName = new AssemblyName(fileNotFoundException.FileName!);
                        if (pluginInfoAttribute.OptionalDependencies.Contains(assemblyName.Name))
                        {
                            continue;
                        }
                    }

                    throw;
                }

                assemblyTypes = e.Types.Where(type => type != null)!;
            }

            return assemblyTypes;
        }

        private bool IsUnloadComplete(Dictionary<WeakReference, string> pendingUnloads, int attempt)
        {
            var retVal = true;
            foreach ((var assemblyReference, var pluginName) in pendingUnloads)
            {
                if (assemblyReference.IsAlive)
                {
                    if (attempt > PluginUnloadAttempts)
                    {
                        Console.WriteLine($"Plugin {pluginName} is preventing unload");
                    }

                    retVal = false;
                }
            }

            return retVal;
        }

        private bool IsValidDependency(string plugin, AssemblyName requested, AssemblyName resolved)
        {
            if (requested.Name != resolved.Name)
            {
                return false;
            }

            if (requested.Version != resolved.Version)
            {
                Console.WriteLine($"DotNET Plugin {plugin} references {requested.Name}, v{requested.Version} but the server is running v{resolved.Version}! You may encounter compatibility issues");
            }

            return true;
        }

        private void LoadCore()
        {
            foreach (var assembly in Assemblies.AllAssemblies)
            {
                _loadedAssemblies.Add(assembly);
            }
        }

        private void LoadPlugin(Plugin plugin)
        {
            Console.WriteLine($"Loading DotNET plugin {plugin.Name.Name} - {plugin.Path}");
            plugin.Load();

            if (plugin.Assembly == null)
            {
                Console.WriteLine($"Failed to load DotNET plugin {plugin.Name.Name} - {plugin.Path}");
                return;
            }

            _loadedAssemblies.Add(plugin.Assembly);
            Console.WriteLine($"Loaded DotNET plugin {plugin.Name.Name} - {plugin.Path}");
        }

        private void LoadPlugins()
        {
            foreach (var plugin in _plugins)
            {
                if (plugin.IsLoaded)
                {
                    continue;
                }

                LoadPlugin(plugin);
            }
        }

        private Assembly? ResolveDependencyFromSwlor(string pluginName, AssemblyName dependencyName)
        {
            foreach (var assembly in Assemblies.AllAssemblies)
            {
                if (IsValidDependency(pluginName, dependencyName, assembly.GetName()))
                {
                    return assembly;
                }
            }

            return null;
        }

        private Assembly? ResolveDependencyFromPlugins(string pluginName, AssemblyName dependencyName)
        {
            foreach (var plugin in _plugins)
            {
                if (!IsValidDependency(pluginName, dependencyName, plugin.Name))
                {
                    continue;
                }

                if (plugin.Loading)
                {
                    Console.WriteLine($"DotNET plugins {pluginName} <--> {plugin.Name.Name} cannot be loaded as they have circular dependencies");
                    return null;
                }

                if (!plugin.IsLoaded)
                {
                    LoadPlugin(plugin);
                }

                return plugin.Assembly;
            }

            return null;
        }
    }
}
