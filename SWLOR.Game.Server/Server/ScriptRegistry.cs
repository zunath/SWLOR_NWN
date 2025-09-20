using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
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
            public MethodInfo MethodInfo { get; set; }
            public bool IsStatic { get; set; }
        }

        private class ConditionalScript
        {
            public ConditionalScriptDelegate Action { get; set; }
            public string Name { get; set; }
            public MethodInfo MethodInfo { get; set; }
            public bool IsStatic { get; set; }
        }


        private const int MaxCharsInScriptName = 16;

        private readonly Dictionary<string, List<ActionScript>> _scripts;
        private readonly Dictionary<string, List<ConditionalScript>> _conditionalScripts;

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public ScriptRegistry(ILogger logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
                    RegisterScript(script, mi);
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

                    RegisterScript(scriptName, method);
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

        private void RegisterScript(string script, MethodInfo methodInfo)
        {
            if (script.Length > MaxCharsInScriptName || script.Length == 0)
            {
                _logger.Write<ErrorLogGroup>($"Script name '{script}' is invalid on method {methodInfo.Name}.");
                throw new ApplicationException();
            }

            if (methodInfo.ReturnType == typeof(bool))
            {
                RegisterConditionalScript(script, methodInfo);
            }
            else if (methodInfo.ReturnType == typeof(void))
            {
                RegisterActionScript(script, methodInfo);
            }
            else
            {
                _logger.Write<ErrorLogGroup>($"Method '{methodInfo.Name}' tied to script '{script}' has an invalid return type. This script was NOT loaded.");
            }
        }

        private void RegisterConditionalScript(string script, MethodInfo methodInfo)
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
                // For non-static methods, we'll handle them differently
            }
            else if (parameters.Length == 1)
            {
                // One parameter - assume it's the event type and create a wrapper
                var eventType = parameters[0].ParameterType;
                del = () => InvokeMethodWithEventBool(methodInfo, eventType);
            }
            else
            {
                _logger.Write<ErrorLogGroup>($"Method '{methodInfo.Name}' tied to script '{script}' has an invalid number of parameters. Expected 0 or 1, got {parameters.Length}. This script was NOT loaded.");
                return;
            }

            if (!_conditionalScripts.ContainsKey(script))
                _conditionalScripts[script] = new List<ConditionalScript>();

            _conditionalScripts[script].Add(new ConditionalScript
            {
                Action = del,
                Name = methodInfo.DeclaringType?.Name + "." + methodInfo.Name,
                MethodInfo = methodInfo,
                IsStatic = methodInfo.IsStatic
            });
        }

        private void RegisterActionScript(string script, MethodInfo methodInfo)
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
                // For non-static methods, we'll handle them differently
            }
            else if (parameters.Length == 1)
            {
                // One parameter - assume it's the event type and create a wrapper
                var eventType = parameters[0].ParameterType;
                del = () => InvokeMethodWithEvent(methodInfo, eventType);
            }
            else
            {
                _logger.Write<ErrorLogGroup>($"Method '{methodInfo.Name}' tied to script '{script}' has an invalid number of parameters. Expected 0 or 1, got {parameters.Length}. This script was NOT loaded.");
                return;
            }

            if (!_scripts.ContainsKey(script))
                _scripts[script] = new List<ActionScript>();

            _scripts[script].Add(new ActionScript
            {
                Action = del,
                Name = methodInfo.DeclaringType?.Name + "." + methodInfo.Name,
                MethodInfo = methodInfo,
                IsStatic = methodInfo.IsStatic
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
                {
                    if (script.IsStatic)
                    {
                        // Static method - use the delegate directly
                        yield return (script.Action, script.Name);
                    }
                    else
                    {
                        // Instance method - create a wrapper that instantiates the class and calls the method
                        yield return (() => InvokeInstanceMethod(script.MethodInfo), script.Name);
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
                        yield return (() => InvokeInstanceMethodBool(script.MethodInfo), script.Name);
                    }
                }
            }
        }

        private void InvokeMethodWithEvent(MethodInfo methodInfo, Type eventType)
        {
            try
            {
                // Create an instance of the event type
                var eventInstance = Activator.CreateInstance(eventType);
                
                // Only support static methods for event handlers with parameters
                if (!methodInfo.IsStatic)
                {
                    _logger.Write<ErrorLogGroup>($"Cannot invoke non-static method '{methodInfo.Name}' with event parameter. Static methods are required for event handlers with parameters. Consider making the method static or removing the parameter.");
                    return;
                }
                
                // Invoke the static method with the event instance
                methodInfo.Invoke(null, new object[] { eventInstance });
            }
            catch (Exception ex)
            {
                _logger.Write<ErrorLogGroup>($"Error invoking method '{methodInfo.Name}' with event parameter: {ex.Message}");
            }
        }

        private bool InvokeMethodWithEventBool(MethodInfo methodInfo, Type eventType)
        {
            try
            {
                // Create an instance of the event type
                var eventInstance = Activator.CreateInstance(eventType);
                
                // Only support static methods for event handlers with parameters
                if (!methodInfo.IsStatic)
                {
                    _logger.Write<ErrorLogGroup>($"Cannot invoke non-static method '{methodInfo.Name}' with event parameter. Static methods are required for event handlers with parameters. Consider making the method static or removing the parameter.");
                    return false;
                }
                
                // Invoke the static method with the event instance and return the result
                var result = methodInfo.Invoke(null, new object[] { eventInstance });
                return result is bool boolResult ? boolResult : false;
            }
            catch (Exception ex)
            {
                _logger.Write<ErrorLogGroup>($"Error invoking method '{methodInfo.Name}' with event parameter: {ex.Message}");
                return false;
            }
        }

        private void InvokeInstanceMethod(MethodInfo methodInfo)
        {
            try
            {
                // Get the instance from the dependency injection container
                var instance = GetServiceInstance(methodInfo.DeclaringType);
                if (instance == null)
                {
                    _logger.Write<ErrorLogGroup>($"Could not resolve instance of type '{methodInfo.DeclaringType.Name}' from dependency injection container for method '{methodInfo.Name}'.");
                    return;
                }
                
                // Invoke the method on the instance
                methodInfo.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                _logger.Write<ErrorLogGroup>($"Error invoking instance method '{methodInfo.Name}': {ex.Message}");
            }
        }

        private bool InvokeInstanceMethodBool(MethodInfo methodInfo)
        {
            try
            {
                // Get the instance from the dependency injection container
                var instance = GetServiceInstance(methodInfo.DeclaringType);
                if (instance == null)
                {
                    _logger.Write<ErrorLogGroup>($"Could not resolve instance of type '{methodInfo.DeclaringType.Name}' from dependency injection container for method '{methodInfo.Name}'.");
                    return false;
                }
                
                // Invoke the method on the instance and return the result
                var result = methodInfo.Invoke(instance, null);
                return result is bool boolResult ? boolResult : false;
            }
            catch (Exception ex)
            {
                _logger.Write<ErrorLogGroup>($"Error invoking instance method '{methodInfo.Name}': {ex.Message}");
                return false;
            }
        }

        private object GetServiceInstance(Type serviceType)
        {
            // First try to get the service by its concrete type
            var instance = _serviceProvider.GetService(serviceType);
            if (instance != null)
                return instance;

            // If not found, try to get it by any interfaces it implements
            var interfaces = serviceType.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                instance = _serviceProvider.GetService(interfaceType);
                if (instance != null && serviceType.IsAssignableFrom(instance.GetType()))
                    return instance;
            }

            return null;
        }
    }
}