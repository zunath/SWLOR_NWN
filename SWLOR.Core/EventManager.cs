using SWLOR.Core.Extension;
using SWLOR.Core.LogService;
using SWLOR.Core.NWNX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWLOR.Core
{
    internal class EventManager
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
        
        private delegate bool ConditionalScriptDelegate();
        private const int MaxCharsInScriptName = 16;

        private static Dictionary<string, List<ActionScript>> _scripts;
        private static Dictionary<string, List<ConditionalScript>> _conditionalScripts;
        
        public void LoadHandlersFromAssembly()
        {
            _scripts = new Dictionary<string, List<ActionScript>>();
            _conditionalScripts = new Dictionary<string, List<ConditionalScript>>();

            var handlers =
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
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

        public bool ConditionalScriptExists(string script)
        {
            return _conditionalScripts.ContainsKey(script);
        }

        public bool ScriptExists(string script)
        {
            return _scripts.ContainsKey(script);
        }

        public bool RunConditionalScript(string script)
        {
            // Always default conditional scripts to true. If one or more of the actions return a false,
            // we will return a false (even if others are true).
            // This ensures all actions get fired when the script is called.
            var result = true;
            
            foreach (var action in _conditionalScripts[script])
            {
                ProfilerPlugin.PushPerfScope(OBJECT_SELF, action.Name);
                var actionResult = action.Action.Invoke();
                ProfilerPlugin.PopPerfScope();

                if (result)
                    result = actionResult;
            }

            return result;
        }

        public void RunScript(string script)
        {
            foreach (var action in _scripts[script])
            {
                try
                {
                    ProfilerPlugin.PushPerfScope(OBJECT_SELF, action.Name);
                    action.Action();
                    ProfilerPlugin.PopPerfScope();
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"C# Script '{script}' threw an exception. Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}");
                }
            }
        }
    }
}
