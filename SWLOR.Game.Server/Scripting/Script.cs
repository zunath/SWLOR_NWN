using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripting
{
    public static class Script
    {

        private static readonly ConcurrentDictionary<string, IScript> _scripts = new ConcurrentDictionary<string, IScript>();
        private static readonly ConcurrentDictionary<int, AbstractQuest> _quests = new ConcurrentDictionary<int, AbstractQuest>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(x =>
            {
                RegisterScripts();
                RegisterQuests();
            });
        }

        private static void RegisterScripts()
        {
            var type = typeof(IScript);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsInterface && !p.IsAbstract);

            Parallel.ForEach(types, t =>
            {
                var scriptNamespace = t.Namespace + "." + t.Name;
                Console.WriteLine("Registering script: " + scriptNamespace);
                _scripts[scriptNamespace] = (IScript)Activator.CreateInstance(t);
                _scripts[scriptNamespace].SubscribeEvents();
            });
        }

        private static void RegisterQuests()
        {

        }

        public static bool IsScriptRegistered(string scriptName)
        {
            string rootNamespace = Assembly.GetExecutingAssembly().GetName().Name;
            string scriptNamespace = rootNamespace + ".Scripting." + scriptName;

            return _scripts.ContainsKey(scriptNamespace);
        }

        public static void RunScript(string scriptName)
        {
            string rootNamespace = Assembly.GetExecutingAssembly().GetName().Name;
            string scriptNamespace = rootNamespace + ".Scripting." + scriptName;

            // Check the script cache first. If it exists, we run it.
            if (IsScriptRegistered(scriptNamespace))
            {
                _scripts[scriptNamespace].Main();
            }
            else
            {
                Console.WriteLine("Unable to locate script: " + scriptNamespace);
            }
        }

    }
}
