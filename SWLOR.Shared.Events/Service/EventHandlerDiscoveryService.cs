using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Contracts;

namespace SWLOR.Shared.Events.Service
{
    /// <summary>
    /// Service responsible for discovering and registering event handlers decorated with ScriptHandler attributes.
    /// </summary>
    public class EventHandlerDiscoveryService : IEventHandlerDiscoveryService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IDisposable> _subscriptions = new();

        public EventHandlerDiscoveryService(IEventAggregator eventAggregator, ILogger logger, IServiceProvider serviceProvider)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void DiscoverAndRegisterHandlers()
        {
            try
            {
                // Get all loaded assemblies that might contain event handlers
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !a.FullName.StartsWith("System.") && !a.FullName.StartsWith("Microsoft."))
                    .ToList();

                var totalTypesProcessed = 0;
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var handlerTypes = assembly.GetTypes()
                            .Where(t => t.IsClass && !t.IsAbstract)
                            .ToList();

                        totalTypesProcessed += handlerTypes.Count;
                        foreach (var handlerType in handlerTypes)
                        {
                            RegisterHandlersFromType(handlerType);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not process assembly {assembly.FullName}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Event handler discovery completed. Processed {totalTypesProcessed} types across {assemblies.Count} assemblies. Registered {_subscriptions.Count} handlers.");
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error during event handler discovery: {ex.Message}");
                throw;
            }
        }

        private void RegisterHandlersFromType(Type handlerType)
        {
            try
            {
                // Get all methods with ScriptHandler attributes
                var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.GetCustomAttributes<Attribute>().Any(attr => attr.GetType().IsGenericType && 
                        attr.GetType().GetGenericTypeDefinition() == typeof(ScriptHandlerAttribute<>)))
                    .ToList();

                if (!methods.Any())
                    return;

                // Create an instance of the handler type if it's not static
                object handlerInstance = null;
                if (!handlerType.IsAbstract)
                {
                    try
                    {
                        // Try to resolve the type from the DI container first
                        handlerInstance = _serviceProvider.GetRequiredService(handlerType);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not create instance of {handlerType.Name}: {ex.Message}");
                        return;
                    }
                }

                foreach (var method in methods)
                {
                    RegisterHandlerMethod(handlerType, method, handlerInstance);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error registering handlers from type {handlerType.Name}: {ex.Message}");
            }
        }

        private void RegisterHandlerMethod(Type handlerType, MethodInfo method, object handlerInstance)
        {
            try
            {
                // Get the ScriptHandler attribute to determine the event type
                var scriptHandlerAttr = method.GetCustomAttributes<Attribute>()
                    .FirstOrDefault(attr => attr.GetType().IsGenericType && 
                        attr.GetType().GetGenericTypeDefinition() == typeof(ScriptHandlerAttribute<>));

                if (scriptHandlerAttr == null)
                    return;

                // Extract the event type from the generic attribute
                var eventType = scriptHandlerAttr.GetType().GetGenericArguments()[0];

                // Create a delegate that calls the method
                var methodDelegate = CreateMethodDelegate(method, handlerInstance, eventType);
                if (methodDelegate == null)
                    return;

                // Subscribe to the event - use conditional subscription for boolean return types
                var returnType = method.ReturnType;
                MethodInfo subscribeMethod;
                
                if (returnType == typeof(bool))
                {
                    subscribeMethod = typeof(IEventAggregator).GetMethod(nameof(IEventAggregator.SubscribeConditional))
                        .MakeGenericMethod(eventType);
                }
                else
                {
                    subscribeMethod = typeof(IEventAggregator).GetMethod(nameof(IEventAggregator.Subscribe))
                        .MakeGenericMethod(eventType);
                }
                
                var subscription = subscribeMethod.Invoke(_eventAggregator, new object[] { methodDelegate }) as IDisposable;

                if (subscription != null)
                {
                    _subscriptions.Add(subscription);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error registering handler method {handlerType.Name}.{method.Name}: {ex.Message}");
            }
        }

        private Delegate CreateMethodDelegate(MethodInfo method, object instance, Type eventType)
        {
            try
            {
                // Check if the method takes the event parameter
                var parameters = method.GetParameters();
                var returnType = method.ReturnType;
                
                if (parameters.Length == 0)
                {
                    // Method takes no parameters
                    if (returnType == typeof(void))
                    {
                        // Void return - create Action delegate
                        var actionDelegate = Delegate.CreateDelegate(typeof(Action), instance, method);
                        // Create a wrapper that takes the event parameter but ignores it
                        return new Action<object>(_ => actionDelegate.DynamicInvoke());
                    }
                    else if (returnType == typeof(bool))
                    {
                        // Boolean return - create Func<bool> delegate
                        var funcDelegate = Delegate.CreateDelegate(typeof(Func<bool>), instance, method);
                        // Create a wrapper that takes the event parameter but ignores it
                        return new Func<object, bool>(_ => (bool)funcDelegate.DynamicInvoke());
                    }
                    else
                    {
                        _logger.WriteError($"Method {method.Name} has unsupported return type {returnType.Name}. Expected void or bool.");
                        return null;
                    }
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == eventType)
                {
                    // Method takes the event parameter
                    if (returnType == typeof(void))
                    {
                        // Void return - create Action<T> delegate
                        var delegateType = typeof(Action<>).MakeGenericType(eventType);
                        var methodDelegate = Delegate.CreateDelegate(delegateType, instance, method);
                        return methodDelegate;
                    }
                    else if (returnType == typeof(bool))
                    {
                        // Boolean return - create Func<T, bool> delegate
                        var delegateType = typeof(Func<,>).MakeGenericType(eventType, typeof(bool));
                        var methodDelegate = Delegate.CreateDelegate(delegateType, instance, method);
                        return methodDelegate;
                    }
                    else
                    {
                        _logger.WriteError($"Method {method.Name} has unsupported return type {returnType.Name}. Expected void or bool.");
                        return null;
                    }
                }
                else
                {
                    _logger.WriteError($"Method {method.Name} has incompatible signature. Expected 0 parameters or 1 parameter of type {eventType.Name}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error creating delegate for method {method.Name}: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}
