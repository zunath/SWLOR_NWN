using System;
using System.Reflection;
using SWLOR.Game.Server.Scripting;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Legacy
{
    public static class ScriptItemEvent
    {
        public static void Run(string script)
        {
            if (!script.StartsWith("Item."))
            {
                script = "Item." + script;
            }

            using (new Profiler(nameof(ScriptEvent) + "." + script))
            {
                string rootNamespace = Assembly.GetExecutingAssembly().GetName().Name;
                string scriptNamespace = rootNamespace + ".Scripts." + script;

                // Check the script cache first. If it exists, we run it.
                if (ScriptService.IsScriptRegisteredByNamespace(scriptNamespace))
                {
                    ScriptService.RunScriptByNamespace(scriptNamespace);
                }
                else
                {
                    Console.WriteLine("Unable to locate item script: " + script);
                }
            }
        }
    }
}
