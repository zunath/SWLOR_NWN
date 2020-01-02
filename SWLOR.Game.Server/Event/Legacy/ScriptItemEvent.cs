using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Scripting;
using SWLOR.Game.Server.Scripting.Contracts;
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
                // Check the script cache first. If it exists, we run it.
                if (Script.IsScriptRegistered(script))
                {
                    Script.RunScript(script);
                }
                else
                {
                    Console.WriteLine("Unable to locate item script: " + script);
                }
            }
        }
    }
}
