using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Shared.Abstractions.Delegates;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Shared.Core.Server
{
    public class ScriptRegistry : IScriptRegistry
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

        private readonly Dictionary<string, List<ActionScript>> _scripts;
        private readonly Dictionary<string, List<ConditionalScript>> _conditionalScripts;

        private readonly ILogger _logger;

        public ScriptRegistry(ILogger logger)
        {
            _logger = logger;
            _scripts = new Dictionary<string, List<ActionScript>>();
            _conditionalScripts = new Dictionary<string, List<ConditionalScript>>();
        }

        public void LoadHandlersFromAssembly()
        {
            _scripts.Clear();
            _conditionalScripts.Clear();

            var handlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(ScriptHandler), false).Length > 0)
                .ToArray();

            foreach (var mi in handlers)
            {
                foreach (var attr in mi.GetCustomAttributes(typeof(ScriptHandler), false))
                {
                    var script = ((ScriptHandler)attr).Script;
                    if (script.Length > MaxCharsInScriptName || script.Length == 0)
                    {
                        _logger.Write<ErrorLogGroup>($"Script name '{script}' is invalid on method {mi.Name}.");
                        throw new ApplicationException();
                    }

                    if (mi.ReturnType == typeof(bool))
                    {
                        RegisterConditionalScript(script, mi);
                    }
                    else if (mi.ReturnType == typeof(void))
                    {
                        RegisterActionScript(script, mi);
                    }
                    else
                    {
                        _logger.Write<ErrorLogGroup>($"Method '{mi.Name}' tied to script '{script}' has an invalid return type. This script was NOT loaded.");
                    }
                }
            }
        }

        private void RegisterConditionalScript(string script, MethodInfo methodInfo)
        {
            var del = (ConditionalScriptDelegate)methodInfo.CreateDelegate(typeof(ConditionalScriptDelegate));

            if (!_conditionalScripts.ContainsKey(script))
                _conditionalScripts[script] = new List<ConditionalScript>();

            _conditionalScripts[script].Add(new ConditionalScript
            {
                Action = del,
                Name = del.Method.DeclaringType?.Name + "." + del.Method.Name
            });
        }

        private void RegisterActionScript(string script, MethodInfo methodInfo)
        {
            var del = (Action)methodInfo.CreateDelegate(typeof(Action));

            if (!_scripts.ContainsKey(script))
                _scripts[script] = new List<ActionScript>();

            _scripts[script].Add(new ActionScript
            {
                Action = del,
                Name = del.Method.DeclaringType?.Name + "." + del.Method.Name
            });
        }

        public bool HasScript(string scriptName)
        {
            return _scripts.ContainsKey(scriptName) || _conditionalScripts.ContainsKey(scriptName);
        }

        public bool HasConditionalScript(string scriptName)
        {
            return _conditionalScripts.ContainsKey(scriptName);
        }

        public IEnumerable<(Action Action, string Name)> GetActionScripts(string scriptName)
        {
            if (_scripts.TryGetValue(scriptName, out var scripts))
            {
                foreach (var script in scripts)
                    yield return (script.Action, script.Name);
            }
        }

        public IEnumerable<(ConditionalScriptDelegate Action, string Name)> GetConditionalScripts(string scriptName)
        {
            if (_conditionalScripts.TryGetValue(scriptName, out var scripts))
            {
                foreach (var script in scripts)
                    yield return (script.Action, script.Name);
            }
        }
    }
}