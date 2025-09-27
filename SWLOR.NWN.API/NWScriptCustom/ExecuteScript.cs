// ReSharper disable once CheckNamespace
namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Make oTarget run sScript and then return execution to the calling script.
        /// If sScript does not specify a compiled script, nothing happens.
        ///
        /// This method automatically chooses between C# and NWScript execution:
        /// - If a C# script exists, it will be executed using proper VM context management
        /// - If no C# script exists, it falls back to NWScript execution
        ///
        /// This solves OBJECT_SELF corruption issues when executing C# scripts by bypassing
        /// the problematic NWScript round-trip.
        /// </summary>
        public static void ExecuteScript(string sScript, uint oTarget)
        {
            var provider = ScriptExecutionProvider.Current;

            // Check if we have a C# script first
            if (provider != null && provider.HasScript(sScript))
            {
                // Execute C# script using proper VM context management to avoid OBJECT_SELF corruption
                provider.ExecuteInScriptContext(() => {
                    foreach (var (action, name) in provider.GetActionScripts(sScript))
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"C# Script '{sScript}' threw an exception: {ex.Message}", ex);
                        }
                    }
                }, oTarget);
            }
            else
            {
                // No C# script found - fallback to NWScript execution
                global::NWN.Core.NWScript.ExecuteScript(sScript, oTarget);
            }
        }
    }
}