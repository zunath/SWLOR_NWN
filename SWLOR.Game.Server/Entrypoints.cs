using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server
{
    public class Entrypoints
    {
        public const int SCRIPT_HANDLED = 0;
        public const int SCRIPT_NOT_HANDLED = -1;

        private delegate bool ConditionalScriptDelegate();
        private static readonly Dictionary<string, Action> _scripts = new Dictionary<string, Action>();
        private static readonly Dictionary<string, ConditionalScriptDelegate> _conditionalScripts = new Dictionary<string, ConditionalScriptDelegate>();
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
            if (_conditionalScripts.ContainsKey(script))
            {
                try
                {
                    return _conditionalScripts[script].Invoke() ? 1 : 0;
                }
                catch (Exception ex)
                {
                    LoggingService.LogError(ex, script);
                    return SCRIPT_HANDLED;
                }
            }
            else if (_scripts.ContainsKey(script))
            {
                try
                {
                    _scripts[script].Invoke();
                    return SCRIPT_HANDLED;
                }
                catch (Exception ex)
                {
                    LoggingService.LogError(ex, script);
                    return SCRIPT_HANDLED;
                }
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
            ConfigureDatabase();
            LoadScripts();
        }

        private static void ConfigureDatabase()
        {
            SqlMapper.AddTypeHandler(new MySqlGuidTypeHandler());
            SqlMapper.RemoveTypeMap(typeof(Guid));
            SqlMapper.RemoveTypeMap(typeof(Guid?));
        }

        private static void LoadScripts()
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
                    if (method.ReturnType == typeof(int))
                    {
                        _conditionalScripts[type.Name] = () =>
                        {
                            var result = (int)method.Invoke(null, null);
                            return result == 1;
                        };
                    }
                    else
                    {
                        _scripts[type.Name] = () => method.Invoke(null, null);
                    }
                }
            }
        }
    }
}