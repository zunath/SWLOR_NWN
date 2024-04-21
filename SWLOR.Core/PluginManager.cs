using System.Reflection;
using SWLOR.Game.Server;

namespace SWLOR.Core
{
    public class PluginManager
    {
        private List<IPlugin> _loadedPlugins = new();
        private List<Assembly> _loadedAssemblies = new();
        private readonly ApplicationSettings _appSettings = ApplicationSettings.Get();

        public void LoadPlugins()
        {
            foreach (var file in Directory.GetFiles(_appSettings.PluginDirectory, "*.dll"))
            {
                var fileName = Path.GetFileName(file);
                
                if (fileName == "SWLOR.Core.dll")
                    continue;


                Console.WriteLine($"Loading plugin: {file}");
                var assembly = Assembly.LoadFile(file);
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    var cleanName = args.Name.Split(",")[0];
                    return AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == cleanName);
                };
                
                _loadedAssemblies.Add(assembly);


                var pluginType = assembly
                    .GetTypes()
                    .SingleOrDefault(w => typeof(IPlugin).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

                if (pluginType == null)
                {
                    Console.WriteLine($"Plugin '{fileName}' could not be loaded. One instance of a class implementing IPlugin must exist.");
                }

                var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                _loadedPlugins.Add(plugin);

                plugin.OnStart();
            }
        }
    }
}
