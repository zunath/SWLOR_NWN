using System;
using System.Collections.Generic;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Delegates;

namespace SWLOR.App.Server.Server
{
    public class ScriptRegistry : IScriptRegistry
    {
        private readonly Dictionary<string, List<ActionScript>> _scripts;
        private readonly Dictionary<string, List<ConditionalScript>> _conditionalScripts;

        private readonly ILogger _logger;
        private readonly ScriptRegistrar _scriptRegistrar;
        private readonly ScriptMethodInvoker _methodInvoker;

        public ScriptRegistry(ILogger logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _methodInvoker = new ScriptMethodInvoker(logger, serviceProvider);
            var nameGenerator = new ScriptNameGenerator();
            _scriptRegistrar = new ScriptRegistrar(logger, _methodInvoker, nameGenerator);
            
            _scripts = new Dictionary<string, List<ActionScript>>();
            _conditionalScripts = new Dictionary<string, List<ConditionalScript>>();
        }

        public void LoadHandlersFromAssembly()
        {
            _scripts.Clear();
            _conditionalScripts.Clear();

            // Load event-based handlers
            var eventScripts = _scriptRegistrar.LoadEventHandlers();
            foreach (var kvp in eventScripts)
            {
                _scripts[kvp.Key] = kvp.Value;
            }

            // Load conditional script handlers
            var conditionalScripts = _scriptRegistrar.LoadConditionalHandlers();
            foreach (var kvp in conditionalScripts)
            {
                _conditionalScripts[kvp.Key] = kvp.Value;
            }
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
                {
                    if (script.IsStatic)
                    {
                        // Static method - use the delegate directly
                        yield return (script.Action, script.Name);
                    }
                    else
                    {
                        // Instance method - create a wrapper that instantiates the class and calls the method
                        yield return (() => _methodInvoker.InvokeInstanceMethod(script.MethodInfo), script.Name);
                    }
                }
            }
        }

        public IEnumerable<(ConditionalScriptDelegate Action, string Name)> GetConditionalScripts(string scriptName)
        {
            if (_conditionalScripts.TryGetValue(scriptName, out var scripts))
            {
                foreach (var script in scripts)
                {
                    if (script.IsStatic)
                    {
                        // Static method - use the delegate directly
                        yield return (script.Action, script.Name);
                    }
                    else
                    {
                        // Instance method - create a wrapper that instantiates the class and calls the method
                        yield return (() => _methodInvoker.InvokeInstanceMethodBool(script.MethodInfo), script.Name);
                    }
                }
            }
        }

    }
}
