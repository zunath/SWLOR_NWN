using System;
using System.Reflection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Server
{
    /// <summary>
    /// Handles invocation of script methods, supporting both static and instance methods.
    /// </summary>
    public class ScriptMethodInvoker
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public ScriptMethodInvoker(ILogger logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Invokes a method with an event parameter.
        /// </summary>
        public void InvokeMethodWithEvent(MethodInfo methodInfo, Type eventType)
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

        /// <summary>
        /// Invokes a method with an event parameter and returns a boolean result.
        /// </summary>
        public bool InvokeMethodWithEventBool(MethodInfo methodInfo, Type eventType)
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
                _logger.Write<ErrorLogGroup>($"Error invoking method '{methodInfo.Name}' with event parameter: {ex.ToMessageAndCompleteStacktrace()}");
                return false;
            }
        }

        /// <summary>
        /// Invokes an instance method without parameters.
        /// </summary>
        public void InvokeInstanceMethod(MethodInfo methodInfo)
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
                _logger.Write<ErrorLogGroup>($"Error invoking instance method '{methodInfo.Name}': {ex.ToMessageAndCompleteStacktrace()}");
            }
        }

        /// <summary>
        /// Invokes an instance method without parameters and returns a boolean result.
        /// </summary>
        public bool InvokeInstanceMethodBool(MethodInfo methodInfo)
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
                _logger.Write<ErrorLogGroup>($"Error invoking instance method '{methodInfo.Name}': {ex.ToMessageAndCompleteStacktrace()}");
                return false;
            }
        }

        /// <summary>
        /// Gets a service instance from the DI container, trying both concrete type and interfaces.
        /// </summary>
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
