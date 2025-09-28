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
    /// <summary>
    /// Handles registration of script methods from assemblies.
    /// </summary>
    public class ScriptRegistrar
    {
        private readonly ILogger _logger;
        private readonly ScriptMethodInvoker _methodInvoker;
        private readonly ScriptNameGenerator _nameGenerator;

        public ScriptRegistrar(ILogger logger, ScriptMethodInvoker methodInvoker, ScriptNameGenerator nameGenerator)
        {
            _logger = logger;
            _methodInvoker = methodInvoker;
            _nameGenerator = nameGenerator;
        }


        /// <summary>
        /// Loads event-based handlers from all assemblies.
        /// </summary>
        /// <returns>Dictionary of script names to their handlers</returns>
        public Dictionary<string, List<ActionScript>> LoadEventHandlers()
        {
            var scripts = new Dictionary<string, List<ActionScript>>();

            var eventHandlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods())
                .Where(HasEventHandlerAttribute)
                .Where(m => m.ReturnType == typeof(void)) // Only void methods for action scripts
                .ToArray();

            foreach (var method in eventHandlers)
            {
                var eventHandlerAttributes = GetEventHandlerAttributes(method);

                foreach (var attr in eventHandlerAttributes)
                {
                    var eventType = GetEventTypeFromAttribute(attr);
                    var scriptName = _nameGenerator.GetScriptNameFromEventType(eventType);

                    if (string.IsNullOrEmpty(scriptName))
                    {
                        _logger.Write<ErrorLogGroup>($"No script name found for event type '{eventType.Name}' on method {method.Name}. This handler was NOT loaded.");
                        continue;
                    }

                    if (!_nameGenerator.IsValidScriptName(scriptName))
                    {
                        _logger.Write<ErrorLogGroup>($"Script name '{scriptName}' is too long for event type '{eventType.Name}' on method {method.Name}. This handler was NOT loaded.");
                        continue;
                    }

                    RegisterScript(scripts, scriptName, method);
                }
            }

            return scripts;
        }

        /// <summary>
        /// Loads conditional script handlers from all assemblies.
        /// </summary>
        /// <returns>Dictionary of script names to their conditional handlers</returns>
        public Dictionary<string, List<ConditionalScript>> LoadConditionalHandlers()
        {
            var conditionalScripts = new Dictionary<string, List<ConditionalScript>>();

            var handlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods())
                .Where(m => m.ReturnType == typeof(bool))
                .Where(m => m.GetCustomAttributes().Any(attr => 
                    attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == typeof(ScriptHandlerAttribute<>)))
                .ToArray();

            foreach (var methodInfo in handlers)
            {
                var eventHandlerAttributes = GetEventHandlerAttributes(methodInfo);
                
                foreach (var attr in eventHandlerAttributes)
                {
                    var eventType = GetEventTypeFromAttribute(attr);
                    var scriptName = _nameGenerator.GetScriptNameFromEventType(eventType);

                    if (string.IsNullOrEmpty(scriptName))
                    {
                        _logger.Write<ErrorLogGroup>($"No script name found for event type '{eventType.Name}' on method {methodInfo.Name}. This handler was NOT loaded.");
                        continue;
                    }

                    if (!_nameGenerator.IsValidScriptName(scriptName))
                    {
                        _logger.Write<ErrorLogGroup>($"Script name '{scriptName}' is too long for event type '{eventType.Name}' on method {methodInfo.Name}. This handler was NOT loaded.");
                        continue;
                    }

                    _logger.Write<InfrastructureLogGroup>($"Registering conditional script '{scriptName}' for method {methodInfo.DeclaringType?.Name}.{methodInfo.Name}");
                    RegisterConditionalScript(conditionalScripts, scriptName, methodInfo);
                }
            }
            return conditionalScripts;
        }

        private void RegisterScript(Dictionary<string, List<ActionScript>> scripts, string script, MethodInfo methodInfo)
        {
            if (!_nameGenerator.IsValidScriptName(script))
            {
                _logger.Write<ErrorLogGroup>($"Script name '{script}' is invalid on method {methodInfo.Name}.");
                return;
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                _logger.Write<ErrorLogGroup>($"Method '{methodInfo.Name}' tied to script '{script}' has an invalid return type. Expected void, got {methodInfo.ReturnType.Name}. This script was NOT loaded.");
                return;
            }

            var actionScript = CreateActionScript(script, methodInfo);
            if (actionScript != null)
            {
                if (!scripts.ContainsKey(script))
                    scripts[script] = new List<ActionScript>();

                scripts[script].Add(actionScript);
            }
        }

        private void RegisterConditionalScript(Dictionary<string, List<ConditionalScript>> conditionalScripts, string script, MethodInfo methodInfo)
        {
            if (!_nameGenerator.IsValidScriptName(script))
            {
                _logger.Write<ErrorLogGroup>($"Script name '{script}' is invalid on method {methodInfo.Name}.");
                return;
            }

            if (methodInfo.ReturnType != typeof(bool))
            {
                _logger.Write<ErrorLogGroup>($"Method '{methodInfo.Name}' tied to script '{script}' has an invalid return type. Expected bool, got {methodInfo.ReturnType.Name}. This script was NOT loaded.");
                return;
            }

            var conditionalScript = CreateConditionalScript(script, methodInfo);
            if (conditionalScript != null)
            {
                if (!conditionalScripts.ContainsKey(script))
                    conditionalScripts[script] = new List<ConditionalScript>();

                conditionalScripts[script].Add(conditionalScript);
            }
        }

        private ActionScript CreateActionScript(string script, MethodInfo methodInfo)
        {
            Action del = null;
            
            // Check if the method has parameters
            var parameters = methodInfo.GetParameters();
            if (parameters.Length == 0)
            {
                // No parameters - use the original approach for static methods
                if (methodInfo.IsStatic)
                {
                    del = (Action)methodInfo.CreateDelegate(typeof(Action));
                }
                else
                {
                    // For non-static methods, create a wrapper that will be handled by ScriptMethodInvoker
                    del = () => _methodInvoker.InvokeInstanceMethod(methodInfo);
                }
            }
            else if (parameters.Length == 1)
            {
                // One parameter - assume it's the event type and create a wrapper
                var eventType = parameters[0].ParameterType;
                del = () => _methodInvoker.InvokeMethodWithEvent(methodInfo, eventType);
            }
            else
            {
                _logger.Write<ErrorLogGroup>($"Method '{methodInfo.Name}' tied to script '{script}' has an invalid number of parameters. Expected 0 or 1, got {parameters.Length}. This script was NOT loaded.");
                return null;
            }

            return new ActionScript
            {
                Action = del,
                Name = methodInfo.DeclaringType?.Name + "." + methodInfo.Name,
                MethodInfo = methodInfo,
                IsStatic = methodInfo.IsStatic
            };
        }

        private ConditionalScript CreateConditionalScript(string script, MethodInfo methodInfo)
        {
            ConditionalScriptDelegate del = null;
            
            // Check if the method has parameters
            var parameters = methodInfo.GetParameters();
            if (parameters.Length == 0)
            {
                // No parameters - use the original approach for static methods
                if (methodInfo.IsStatic)
                {
                    del = (ConditionalScriptDelegate)methodInfo.CreateDelegate(typeof(ConditionalScriptDelegate));
                }
                else
                {
                    // For non-static methods, create a wrapper that will be handled by ScriptMethodInvoker
                    del = () => _methodInvoker.InvokeInstanceMethodBool(methodInfo);
                }
            }
            else if (parameters.Length == 1)
            {
                // One parameter - assume it's the event type and create a wrapper
                var eventType = parameters[0].ParameterType;
                del = () => _methodInvoker.InvokeMethodWithEventBool(methodInfo, eventType);
            }
            else
            {
                _logger.Write<ErrorLogGroup>($"Method '{methodInfo.Name}' tied to script '{script}' has an invalid number of parameters. Expected 0 or 1, got {parameters.Length}. This script was NOT loaded.");
                return null;
            }

            return new ConditionalScript
            {
                Action = del,
                Name = methodInfo.DeclaringType?.Name + "." + methodInfo.Name,
                MethodInfo = methodInfo,
                IsStatic = methodInfo.IsStatic
            };
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
    }
}
