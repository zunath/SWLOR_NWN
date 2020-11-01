using System;
using System.Reflection;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Event.Legacy
{
    public static class ScriptEvents
    {
        [NWNEventHandler("script_1")]
        public static void Script1()
        {
            Run("SCRIPT_1");
        }

        [NWNEventHandler("script_2")]
        public static void Script2()
        {
            Run("SCRIPT_2");
        }

        [NWNEventHandler("script_3")]
        public static void Script3()
        {
            Run("SCRIPT_3");
        }

        [NWNEventHandler("script_4")]
        public static void Script4()
        {
            Run("SCRIPT_4");
        }

        [NWNEventHandler("script_5")]
        public static void Script5()
        {
            Run("SCRIPT_5");
        }

        [NWNEventHandler("script_6")]
        public static void Script6()
        {
            Run("SCRIPT_6");
        }

        [NWNEventHandler("script_7")]
        public static void Script7()
        {
            Run("SCRIPT_7");
        }

        [NWNEventHandler("script_8")]
        public static void Script8()
        {
            Run("SCRIPT_8");
        }

        [NWNEventHandler("script_9")]
        public static void Script9()
        {
            Run("SCRIPT_9");
        }

        private static void Run(string variableName)
        {
            NWObject self = (NWScript.OBJECT_SELF);
            var script = self.GetLocalString(variableName);
            var rootNamespace = Assembly.GetExecutingAssembly().GetName().Name;
            var scriptNamespace = rootNamespace + ".Scripts." + script;

            // Check the script cache first. If it exists, we run it.
            if (ScriptService.IsScriptRegisteredByNamespace(scriptNamespace))
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
