using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Delegates;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Events.Attributes;

namespace SWLOR.Game.Server.Server
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

            // Load traditional script handlers
            LoadTraditionalHandlers();
            
            // Load event-based handlers
            LoadEventHandlers();
        }

        private void LoadTraditionalHandlers()
        {
            var handlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(ScriptHandlerAttribute), false).Length > 0)
                .ToArray();

            foreach (var mi in handlers)
            {
                foreach (var attr in mi.GetCustomAttributes(typeof(ScriptHandlerAttribute), false))
                {
                    var script = ((ScriptHandlerAttribute)attr).Script;
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

        private void LoadEventHandlers()
        {
            var eventHandlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods())
                .Where(m => HasEventHandlerAttribute(m))
                .ToArray();

            foreach (var method in eventHandlers)
            {
                var eventHandlerAttributes = GetEventHandlerAttributes(method);
                
                foreach (var attr in eventHandlerAttributes)
                {
                    var eventType = GetEventTypeFromAttribute(attr);
                    var scriptName = GetScriptNameFromEventType(eventType);

                    if (string.IsNullOrEmpty(scriptName))
                    {
                        _logger.Write<ErrorLogGroup>($"No script name found for event type '{eventType.Name}' on method {method.Name}. This handler was NOT loaded.");
                        continue;
                    }

                    if (scriptName.Length > MaxCharsInScriptName)
                    {
                        _logger.Write<ErrorLogGroup>($"Script name '{scriptName}' is too long for event type '{eventType.Name}' on method {method.Name}. This handler was NOT loaded.");
                        continue;
                    }

                    if (method.ReturnType == typeof(bool))
                    {
                        RegisterConditionalScript(scriptName, method);
                    }
                    else if (method.ReturnType == typeof(void))
                    {
                        RegisterActionScript(scriptName, method);
                    }
                    else
                    {
                        _logger.Write<ErrorLogGroup>($"Method '{method.Name}' tied to event type '{eventType.Name}' has an invalid return type. This handler was NOT loaded.");
                    }
                }
            }
        }

        private bool HasEventHandlerAttribute(MethodInfo method)
        {
            return method.GetCustomAttributes()
                .Any(attr => attr.GetType().IsGenericType && 
                            attr.GetType().GetGenericTypeDefinition() == typeof(ScriptHandlerAttribute<>));
        }

        private IEnumerable<Attribute> GetEventHandlerAttributes(MethodInfo method)
        {
            return method.GetCustomAttributes()
                .Where(attr => attr.GetType().IsGenericType && 
                              attr.GetType().GetGenericTypeDefinition() == typeof(ScriptHandlerAttribute<>));
        }

        private Type GetEventTypeFromAttribute(Attribute attr)
        {
            return attr.GetType().GetGenericArguments()[0];
        }

        private string GetScriptNameFromEventType(Type eventType)
        {
            // Convert event type name to script name using a simple convention
            // OnModuleLoad -> mod_load
            // OnPlayerDamaged -> pc_damaged
            // etc.
            
            var eventName = eventType.Name;
            
            // Remove "On" prefix if present
            if (eventName.StartsWith("On"))
            {
                eventName = eventName.Substring(2);
            }
            
            // Convert PascalCase to snake_case
            var scriptName = ConvertToSnakeCase(eventName);
            
            // Ensure it's within the 16-character limit
            if (scriptName.Length > MaxCharsInScriptName)
            {
                scriptName = scriptName.Substring(0, MaxCharsInScriptName);
            }
            
            return scriptName;
        }

        private string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(input[i]));
                }
                else
                {
                    result.Append(input[i]);
                }
            }

            return result.ToString();
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