using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.App.Server.Server
{
    /// <summary>
    /// Maps NWN script names to their corresponding event types.
    /// This is used to bridge NWN script calls to the IEventAggregator.
    /// </summary>
    public class ScriptToEventMapper
    {
        private readonly Dictionary<string, Type> _scriptToEventType = new();
        private readonly ILogger _logger;

        public ScriptToEventMapper(ILogger logger)
        {
            _logger = logger;
            BuildEventMap();
        }

        /// <summary>
        /// Scans all assemblies for event types and builds a mapping from script names to event types.
        /// </summary>
        private void BuildEventMap()
        {
            var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip assemblies that can't be fully loaded
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => typeof(IEvent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToArray();

            foreach (var eventType in eventTypes)
            {
                try
                {
                    // Create an instance to get the script name
                    var eventInstance = Activator.CreateInstance(eventType) as IEvent;
                    if (eventInstance?.Script != null)
                    {
                        var scriptName = eventInstance.Script;
                        
                        // Check for duplicates
                        if (_scriptToEventType.ContainsKey(scriptName))
                        {
                            _logger.Write<InfrastructureLogGroup>($"Duplicate script name '{scriptName}' found for event types {_scriptToEventType[scriptName].Name} and {eventType.Name}. Using {_scriptToEventType[scriptName].Name}.");
                        }
                        else
                        {
                            _scriptToEventType[scriptName] = eventType;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Write<InfrastructureLogGroup>($"Failed to instantiate event type '{eventType.Name}' for script mapping: {ex.Message}");
                }
            }

            _logger.Write<InfrastructureLogGroup>($"Mapped {_scriptToEventType.Count} script names to event types.");
        }

        /// <summary>
        /// Gets the event type for the specified script name.
        /// </summary>
        /// <param name="scriptName">The script name</param>
        /// <returns>The event type, or null if not found</returns>
        public Type GetEventType(string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName))
                return null;
                
            _scriptToEventType.TryGetValue(scriptName, out var eventType);
            return eventType;
        }

        /// <summary>
        /// Checks if an event type is registered for the specified script name.
        /// </summary>
        /// <param name="scriptName">The script name</param>
        /// <returns>True if an event type is registered, false otherwise</returns>
        public bool HasEventType(string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName))
                return false;
                
            return _scriptToEventType.ContainsKey(scriptName);
        }
    }
}

