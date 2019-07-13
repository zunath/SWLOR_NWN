using System;
using System.Reflection;
using SWLOR.Game.Server.Scripting;
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
                string scriptNamespace = rootNamespace + ".Scripting.Scripts." + script;

                // Check the script cache first. If it exists, we run it.
                if (ScriptService.IsScriptRegisteredByNamespace(scriptNamespace))
                {
                    ScriptService.RunScriptByNamespace(scriptNamespace);
                    return;
                }

                // Otherwise look for a script contained by the app.
                scriptNamespace = rootNamespace + "." + script;
                Type type = Type.GetType(scriptNamespace);

                if (type == null)
                {
                    Console.WriteLine("Unable to locate type for ScriptItemEvent: " + script);
                    return;
                }

                IRegisteredEvent @event = Activator.CreateInstance(type) as IRegisteredEvent;
                @event?.Run();
            }
        }
    }
}
