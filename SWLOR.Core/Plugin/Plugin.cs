using System.Reflection;

namespace SWLOR.Core.Plugin
{
    internal sealed class Plugin
    {
        private readonly PluginManager _pluginManager;

        private PluginLoadContext? _pluginLoadContext;

        public Plugin(PluginManager pluginManager, string path)
        {
            this._pluginManager = pluginManager;
            Path = path;
            Name = AssemblyName.GetAssemblyName(path);
        }

        public Dictionary<string, string>? AdditionalAssemblyPaths { get; init; }

        public Assembly? Assembly { get; private set; }

        public bool HasResourceDirectory => ResourcePath != null && Directory.Exists(ResourcePath);

        public bool IsLoaded => Assembly != null;

        public bool Loading { get; private set; }

        public AssemblyName Name { get; }

        public string Path { get; }

        public string? ResourcePath { get; init; }

        public Dictionary<string, string>? UnmanagedAssemblyPaths { get; init; }

        public void Load()
        {
            _pluginLoadContext = new PluginLoadContext(_pluginManager, this);
            Loading = true;

            try
            {
                Assembly = _pluginLoadContext.LoadFromAssemblyName(Name);
            }
            finally
            {
                Loading = false;
            }
        }

        public WeakReference Unload()
        {
            Assembly = null;
            var unloadHandle = new WeakReference(_pluginLoadContext, true);
            //if (EnvironmentConfig.ReloadEnabled)
            if(true)
            {
                _pluginLoadContext?.Dispose();
            }

            _pluginLoadContext = null;
            return unloadHandle;
        }
    }
}