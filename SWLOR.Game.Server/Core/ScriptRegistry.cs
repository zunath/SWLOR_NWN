using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Core
{
    public class ScriptRegistry
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

        public delegate bool ConditionalScriptDelegate();

        private const int MaxCharsInScriptName = 16;

        private readonly Dictionary<string, List<ActionScript>> _scripts;
        private readonly Dictionary<string, List<ConditionalScript>> _conditionalScripts;

        public ScriptRegistry()
        {
            _scripts = new Dictionary<string, List<ActionScript>>();
            _conditionalScripts = new Dictionary<string, List<ConditionalScript>>();
        }

        public void LoadHandlersFromAssembly()
        {
            LoadHandlersFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        }

        /// <summary>
        /// Discovers and registers every <see cref="NWNEventHandler"/> method in the given assemblies, in the
        /// order the assemblies are given. <see cref="LoadHandlersFromAssembly"/> calls this with every assembly
        /// loaded in the current AppDomain, which is what the server does at boot; tests call it with an explicit
        /// assembly list so they do not depend on what else the test host has loaded.
        /// </summary>
        public void LoadHandlersFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            _scripts.Clear();
            _conditionalScripts.Clear();

            var handlers = assemblies
                .SelectMany(a => a.GetTypes())
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
                        Log.Write(LogGroup.Error, $"Script name '{script}' is invalid on method {mi.Name}.", true);
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
                        Log.Write(LogGroup.Error, $"Method '{mi.Name}' tied to script '{script}' has an invalid return type. This script was NOT loaded.", true);
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

        /// <summary>
        /// Names of every script that has at least one action (void) handler, in the order the registry stores them.
        /// </summary>
        public IReadOnlyList<string> GetActionScriptNames()
        {
            return _scripts.Keys.ToArray();
        }

        /// <summary>
        /// Names of every script that has at least one conditional (bool) handler, in the order the registry stores them.
        /// </summary>
        public IReadOnlyList<string> GetConditionalScriptNames()
        {
            return _conditionalScripts.Keys.ToArray();
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
