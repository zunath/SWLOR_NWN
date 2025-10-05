using System;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Game.Server.Server
{
    /// <summary>
    /// Generates script names from event types using a consistent naming convention.
    /// </summary>
    public class ScriptNameGenerator
    {
        private const int MaxCharsInScriptName = 16;

        /// <summary>
        /// Converts an event type to a script name using the ScriptName property.
        /// </summary>
        /// <param name="eventType">The event type to convert</param>
        /// <returns>The script name, or null if conversion fails</returns>
        public string GetScriptNameFromEventType(Type eventType)
        {
            if (eventType == null)
                return null;

            try
            {
                // Create an instance of the event type to get its ScriptName
                var eventInstance = Activator.CreateInstance(eventType) as IEvent;
                if (eventInstance?.Script != null)
                {
                    var scriptName = eventInstance.Script;
                    
                    // Ensure it's within the 16-character limit
                    if (scriptName.Length > MaxCharsInScriptName)
                    {
                        scriptName = scriptName.Substring(0, MaxCharsInScriptName);
                    }
                    
                    return scriptName;
                }
            }
            catch (Exception ex)
            {
                // If we can't create an instance, this is an error since all events should implement ScriptName
                throw new InvalidOperationException($"Failed to get ScriptName from event type '{eventType.Name}'. All event types must implement the ScriptName property.", ex);
            }

            return null;
        }


        /// <summary>
        /// Validates that a script name is within the allowed length limits.
        /// </summary>
        /// <param name="scriptName">The script name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidScriptName(string scriptName)
        {
            return !string.IsNullOrEmpty(scriptName) && scriptName.Length <= MaxCharsInScriptName;
        }
    }
}
