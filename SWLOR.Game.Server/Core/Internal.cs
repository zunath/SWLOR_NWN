using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Core
{
    public static class Internal
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

        public static event Action OnScriptContextBegin;
        public static event Action OnScriptContextEnd;

        public static int Bootstrap(IntPtr nativeHandlesPtr, int nativeHandlesLength)
        {
            var retVal = NWNCore.Init(nativeHandlesPtr, nativeHandlesLength, out CoreGameManager coreGameManager);
            coreGameManager.OnSignal += OnSignal;
            coreGameManager.OnServerLoop += OnServerLoop;
            coreGameManager.OnRunScript += OnRunScript;

            Console.WriteLine("Registering loggers...");
            Log.Register();
            Console.WriteLine("Loggers registered successfully.");

            Console.WriteLine("Registering scripts...");
            LoadHandlersFromAssembly();
            Console.WriteLine("Scripts registered successfully.");

            return retVal;
        }

        private static void OnRunScript(string scriptName, uint objectSelf, out int scriptHandlerResult)
        {
            var retVal = RunScripts(scriptName);

            scriptHandlerResult = retVal == -1 ? ScriptNotHandled : retVal;
        }

        private static void OnSignal(string signal)
        {

        }

        private static void OnServerLoop(ulong frame)
        {
            OnScriptContextBegin?.Invoke();

            try
            {
                NwTask.MainThreadSynchronizationContext.Update();
                Scheduler.Process();
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, ex.ToMessageAndCompleteStacktrace());
            }

            OnScriptContextEnd?.Invoke();
        }


        private static int RunScripts(string script)
        {
            if (_conditionalScripts.ContainsKey(script))
            {
                // Always default conditional scripts to true. If one or more of the actions return a false,
                // we will return a false (even if others are true).
                // This ensures all actions get fired when the script is called.
                var result = true;

                if (_conditionalScripts.ContainsKey(script))
                {
                    foreach (var action in _conditionalScripts[script])
                    {
                        var actionResult = action.Action.Invoke();
                        if (result) 
                            result = actionResult;
                    }
                }

                return result ? 1 : 0;
            }
            else if (_scripts.ContainsKey(script))
            {
                if (_scripts.ContainsKey(script))
                {
                    foreach (var action in _scripts[script])
                    {
                        try
                        {
                            action.Action();
                        }
                        catch (Exception ex)
                        {
                            Log.Write(LogGroup.Error, $"C# Script '{script}' threw an exception. Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}");
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
                        Log.Write(LogGroup.Error, $"Script name '{script}' is invalid on method {mi.Name}.");
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
                    else if (mi.ReturnType == typeof(void))
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
    }
}
