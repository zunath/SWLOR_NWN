namespace SWLOR.Core.Plugin
{
    internal sealed class LocalPluginSource : IPluginSource
    {
        private const string PluginResourceDir = "resources";

        private readonly PluginManager _pluginManager;
        private readonly string _rootPath;

        public LocalPluginSource(PluginManager pluginManager, string rootPath)
        {
            this._pluginManager = pluginManager;
            this._rootPath = rootPath;
        }

        public IEnumerable<Plugin> Bootstrap()
        {
            var pluginPaths = Directory.GetDirectories(_rootPath);

            Console.WriteLine($"Loading {pluginPaths.Length} DotNET plugin/s from: {_rootPath}");
            return CreatePluginsFromPaths(pluginPaths);
        }

        private IEnumerable<Plugin> CreatePluginsFromPaths(IEnumerable<string> pluginPaths)
        {
            var plugins = new List<Plugin>();
            foreach (var pluginRoot in pluginPaths)
            {
                var pluginName = new DirectoryInfo(pluginRoot).Name;

                if (Assemblies.IsReservedName(pluginName))
                {
                    Console.WriteLine($"Skipping plugin {pluginName} as it uses a reserved name");
                    continue;
                }

                var pluginPath = Path.Combine(pluginRoot, $"{pluginName}.dll");
                if (!File.Exists(pluginPath))
                {
                    Console.WriteLine($"Cannot find plugin assembly {pluginPath}. Does your plugin assembly match the name of the directory?");
                    continue;
                }

                var plugin = new Plugin(_pluginManager, pluginPath)
                {
                    ResourcePath = Path.Combine(pluginRoot, Path.Combine(pluginRoot, PluginResourceDir)),
                };

                plugins.Add(plugin);
            }

            return plugins;
        }
    }
}