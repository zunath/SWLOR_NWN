using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Legacy.Scripts;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class ScriptService
    {
        private static readonly Dictionary<string, Action> _scripts = new Dictionary<string, Action>();

        public static void Initialize()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IScript).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IScript) Activator.CreateInstance(type);

                instance.SubscribeEvents();
                _scripts[type.FullName] = () => instance.Main();
            }
        }

        public static bool IsScriptRegisteredByNamespace(string scriptNamespace)
        {
            return _scripts.ContainsKey(scriptNamespace);
        }

        public static void RunScriptByNamespace(string scriptNamespace)
        {
            _scripts[scriptNamespace]();
        }
    }
}
