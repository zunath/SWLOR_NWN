using System;
using System.Reflection;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Legacy
{
    public static class ScriptEvent
    {
        public static void Run(string variableName)
        {
            NWObject self = (NWGameObject.OBJECT_SELF);
            string script = self.GetLocalString(variableName);

            using (new Profiler("ScriptEvent." + script))
            {
                string rootNamespace = Assembly.GetExecutingAssembly().GetName().Name;
                string scriptNamespace = rootNamespace + ".Scripts." + script;
                
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
