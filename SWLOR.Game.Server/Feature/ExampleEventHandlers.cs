using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Example event handlers demonstrating the simplified event system.
    /// This shows how components can register event handlers using ScriptHandlerAttribute
    /// without needing to know about NWN script names.
    /// </summary>
    public class ExampleEventHandlers
    {
        /// <summary>
        /// Example handler using the generic ScriptHandlerAttribute with automatic script name derivation.
        /// This will automatically convert OnModuleLoad -> mod_load script name.
        /// </summary>
        [ScriptHandlerAttribute<OnModuleLoad>]
        public void HandleModuleLoad(OnModuleLoad evt)
        {
            // This method will be automatically wired to the NWN script
            // OnModuleLoad -> mod_load (removes "On" prefix, converts to snake_case)
            // No need to know about the 16-character script name!
        }

        /// <summary>
        /// Example handler using the traditional ScriptHandlerAttribute for comparison.
        /// This still works for cases where you need direct control over script names.
        /// </summary>
        [ScriptHandler("mod_load")]
        public void HandleModuleLoadTraditional()
        {
            // Traditional approach - you need to know the exact script name
            // This is still useful for NWN-specific events or custom scripts
        }
    }
}
