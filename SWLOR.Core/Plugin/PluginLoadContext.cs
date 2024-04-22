using System.Reflection;
using System.Runtime.Loader;

namespace SWLOR.Core.Plugin
{
    internal sealed class PluginLoadContext : AssemblyLoadContext, IDisposable
    {
        private static readonly string[] NativeLibPrefixes = { "lib" };
        private readonly Dictionary<string, WeakReference<Assembly>> assemblyCache = new Dictionary<string, WeakReference<Assembly>>();

        private Plugin? plugin;

        private AssemblyDependencyResolver resolver;

        public PluginLoadContext(Plugin plugin) : base(true)
        {
            this.plugin = plugin;

            resolver = new AssemblyDependencyResolver(plugin.Path);
        }

        public void Dispose()
        {
            resolver = null!;
            plugin = null;
            assemblyCache.Clear();
            Unload();
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (!assemblyCache.TryGetValue(assemblyName.FullName, out WeakReference<Assembly>? assemblyRef) || !assemblyRef.TryGetTarget(out Assembly? assembly))
            {
                assembly = GetAssembly(assemblyName);
                if (assembly == null)
                {
                    return assembly;
                }

                assemblyRef = new WeakReference<Assembly>(assembly);
                assemblyCache[assemblyName.FullName] = assemblyRef;
            }

            return assembly;
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            if (plugin == null)
            {
                return nint.Zero;
            }

            string? libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            libraryPath = ResolveUnmanagedFromAdditionalPaths(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return nint.Zero;
        }

        private Assembly? GetAssembly(AssemblyName assemblyName)
        {
            if (plugin?.Name.Name == null)
            {
                return null;
            }

            // Resolve this plugin's assembly locally.
            if (assemblyName.Name == plugin.Name.Name)
            {
                return ResolveLocal(assemblyName);
            }

            // Resolve the dependency with the bundled assemblies (NWN.Core/Anvil), then check if other plugins can provide the dependency.
            Assembly? assembly = PluginManager.ResolveDependency(plugin.Name.Name, assemblyName);
            if (assembly != null)
            {
                return assembly;
            }

            // Then try resolving the dependency locally by checking the plugin folder.
            assembly = ResolveLocal(assemblyName);
            if (assembly != null)
            {
                return assembly;
            }

            // Then try resolving from any specified linked roots.
            return ResolveFromAdditionalPaths(assemblyName);
        }

        private Assembly? ResolveFromAdditionalPaths(AssemblyName assemblyName)
        {
            if (plugin?.AdditionalAssemblyPaths != null && plugin.AdditionalAssemblyPaths.TryGetValue(assemblyName.Name!, out string? assemblyPath))
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        private Assembly? ResolveLocal(AssemblyName assemblyName)
        {
            string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        private string? ResolveUnmanagedFromAdditionalPaths(string unmanagedDllName)
        {
            if (plugin?.UnmanagedAssemblyPaths == null)
            {
                return null;
            }

            if (plugin.UnmanagedAssemblyPaths.TryGetValue(unmanagedDllName, out string? path))
            {
                return path;
            }

            foreach (string nativeLibPrefix in NativeLibPrefixes)
            {
                if (plugin.UnmanagedAssemblyPaths.TryGetValue(nativeLibPrefix + unmanagedDllName, out path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
