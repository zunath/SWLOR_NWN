using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Core
{
    public class Entrypoints
    {
        private class ActionScript
        {
            public Action Action { get; set; }
            public string Name { get; set; }
        }

        private class ConditionalScript
        {
            public ConditionalScriptDelegate Action { get; set; }
            public string Name { get; set; }
        }

        private const int MaxCharsInScriptName = 16;
        private const int ScriptHandled = 0;
        private const int ScriptNotHandled = -1;

        private delegate bool ConditionalScriptDelegate();

        private static Dictionary<string, List<ActionScript>> _scripts;
        private static Dictionary<string, List<ConditionalScript>> _conditionalScripts;

        private static readonly NWTask.TaskRunner _taskRunner = new NWTask.TaskRunner();
        public static event Action OnScriptContextBegin;
        public static event Action OnScriptContextEnd;

        //
        // This is called once every main loop frame, outside of object context
        //
        public static void OnMainLoop(ulong frame)
        {
            OnScriptContextBegin?.Invoke();

            try
            {
                using (new Profiler($"{nameof(Entrypoints)}:TaskRunner"))
                {
                    _taskRunner.Process();
                }

                using (new Profiler($"{nameof(Entrypoints)}:Scheduler.Process()"))
                {
                    Scheduler.Process();
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, ex.ToMessageAndCompleteStacktrace());
            }

            OnScriptContextEnd?.Invoke();
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
            var retVal = RunScripts(script);

            if (retVal == -1) return ScriptNotHandled;
            else return retVal;
        }

        //
        // This is called once when the internal structures have been initialized
        // The module is not yet loaded, so most NWScript functions will fail if
        // called here.
        //
        public static void OnStart()
        {
            using (new Profiler(nameof(OnStart)))
            {
                Console.WriteLine("Registering scripts...");
                LoadHandlersFromAssembly();
                Console.WriteLine("Scripts registered successfully.");
            }
        }

        //
        // This is called once, just before the module load script is called.
        // Unlike OnStart, NWScript functions are available to use here.
        //
        public static void OnModuleLoad()
        {
            Console.WriteLine("OnModuleLoad() called");
            Scheduler.ScheduleRepeating(RunOneSecondPCIntervalEvent, TimeSpan.FromSeconds(1));
        }

        //
        // This is called once, just before the server will shutdown. In here, you should
        // save anything that might not be flushed to disk, and perform any last cleanup.
        // NWScript functions are available to use.
        //
        public static void OnShutdown()
        {
            Console.WriteLine("OnShutdown() called");
        }


        private static int RunScripts(string script)
        {
            if (_conditionalScripts.ContainsKey(script) )
            {
                // Always default conditional scripts to true. If one or more of the actions return a false,
                // we will return a false (even if others are true).
                // This ensures all actions get fired when the script is called.
                var result = true;

                if (_conditionalScripts.ContainsKey(script))
                {
                    foreach (var action in _conditionalScripts[script])
                    {
                        using (new Profiler(action.Name))
                        {
                            var actionResult = action.Action.Invoke();
                            if (result) result = actionResult;
                        }
                    }
                }

                return result ? 1 : 0;
            }
            else if (_scripts.ContainsKey(script) )
            {
                if (_scripts.ContainsKey(script))
                {
                    foreach (var action in _scripts[script])
                    {
                        try
                        {
                            using (new Profiler(action.Name))
                            {
                                action.Action();
                            }
                        }
                        catch (Exception ex)
                        {
                            var details = ex.ToMessageAndCompleteStacktrace();
                            Console.WriteLine($"C# Script '{script}' threw an exception. Details: {Environment.NewLine}{Environment.NewLine}{details}");
                        }
                    }
                }

                return ScriptHandled;
            }

            return ScriptNotHandled;
        }


        private static void LoadHandlersFromAssembly()
        {
            _scripts = new Dictionary<string, List<ActionScript>>();
            _conditionalScripts = new Dictionary<string, List<ConditionalScript>>();

            var handlers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(NWNEventHandler), false).Length > 0)
                .ToArray();

            foreach (var mi in handlers)
            {
                foreach (var attr in mi.GetCustomAttributes(typeof(NWNEventHandler), false))
                {
                    var script = ((NWNEventHandler)attr).Script;
                    if (script.Length > MaxCharsInScriptName || script.Length == 0)
                    {
                        Console.WriteLine($"Script name '{script}' is invalid on method {mi.Name}.");
                        throw new ApplicationException();
                    }

                    // If the return type is a bool, it is assumed to be a conditional script.
                    if (mi.ReturnType == typeof(bool))
                    {
                        var del = (ConditionalScriptDelegate)mi.CreateDelegate(typeof(ConditionalScriptDelegate));

                        if (!_conditionalScripts.ContainsKey(script))
                            _conditionalScripts[script] = new List<ConditionalScript>();

                        _conditionalScripts[script].Add(new ConditionalScript
                        {
                            Action = del,
                            Name = del.Method.DeclaringType?.Name + "." + del.Method.Name
                        });
                    }
                    // Otherwise it's a normal script.
                    else if(mi.ReturnType == typeof(void))
                    {
                        var del = (Action)mi.CreateDelegate(typeof(Action));

                        if (!_scripts.ContainsKey(script))
                            _scripts[script] = new List<ActionScript>();

                        _scripts[script].Add(new ActionScript
                        {
                            Action = del,
                            Name = del.Method.DeclaringType?.Name + "." + del.Method.Name
                        });
                    }
                    else
                    {
                        Log.Write(LogGroup.Error, $"Method '{mi.Name}' tied to script '{script}' has an invalid return type. This script was NOT loaded.", true);
                    }

                }
            }
        }

        /// <summary>
        /// Fires an event on every player every second.
        /// We do it this way so we don't run into a situation
        /// where we iterate over the player list more than once per second
        /// </summary>
        private static void RunOneSecondPCIntervalEvent()
        {
            using (new Profiler(nameof(RunOneSecondPCIntervalEvent)))
            {
                for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                {
                    var oldObjectSelf = Internal.OBJECT_SELF;
                    Internal.OBJECT_SELF = player;
                    RunScripts("interval_pc_1s");
                    Internal.OBJECT_SELF = oldObjectSelf;
                }
            }
        }
    }
}