using System;
using System.Reflection;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Legacy
{
    public static class ScriptEvent
    {
        public static void Run(string variableName)
        {
            NWObject self = (NWScript.OBJECT_SELF);
            var script = self.GetLocalString(variableName);

            using (new Profiler("ScriptEvent." + script))
            {
                var rootNamespace = Assembly.GetExecutingAssembly().GetName().Name;
                var scriptNamespace = rootNamespace + ".Scripts." + script;
                
                // Check the script cache first. If it exists, we run it.
                if(ScriptService.IsScriptRegisteredByNamespace(scriptNamespace))
                {
                    ScriptService.RunScriptByNamespace(scriptNamespace);
                }
                else
                {
                    Console.WriteLine("Unable to locate script: " + scriptNamespace);
                }
            }
        }
    }
}
