using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server
{
    public class Entrypoints
    {
        public const int SCRIPT_HANDLED = 0;
        public const int SCRIPT_NOT_HANDLED = -1;

        private static readonly Dictionary<string, MethodInfo> _scripts = new Dictionary<string, MethodInfo>();
        public static event EventHandler<ulong> MainLoopTick;

        //
        // This is called once every main loop frame, outside of object context
        //
        public static void OnMainLoop(ulong frame)
        {
            MainLoopTick?.Invoke(null, frame);
        }

        //
        // This is called every time a named script is scheduled to run.
        // oidSelf is the object running the script (OBJECT_SELF), and script
        // is the name given to the event handler (e.g. via SetEventScript).
        // If the script is not handled in the managed code, and needs to be
        // forwarded to the original NWScript VM, return SCRIPT_NOT_HANDLED.
        // Otherwise, return either 0/SCRIPT_HANDLED for void main() scripts,
        // or an int (0 or 1) for StartingConditionals
        //
        public static int OnRunScript(string script, uint oidSelf)
        {
            if (_scripts.ContainsKey(script))
            {
                _scripts[script].Invoke(null,null);

                return SCRIPT_HANDLED;
            }

            return SCRIPT_NOT_HANDLED; 
        }

        //
        // This is called once when the internal structures have been initialized
        // The module is not yet loaded, so most NWScript functions will fail if
        // called here.
        //
        public static void OnStart()
        {
            const string Namespace = "NWN.Scripts";

            var types = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.Namespace == Namespace
                select t;

            foreach (var type in types)
            {
                var method = type.GetMethod("Main");
                if (method != null)
                {
                    Console.WriteLine($"type name = {type.Name}");
                    _scripts[type.Name] = method;
                }
            }
        }
    }
}