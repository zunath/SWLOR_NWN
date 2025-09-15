namespace SWLOR.NWN.API
{
    /// <summary>
    /// Interface for providing script execution capabilities to the NWScript extensions.
    /// This allows the API layer to execute scripts without having direct dependencies
    /// on the game server implementation.
    /// </summary>
    public interface IScriptExecutionProvider
    {
        /// <summary>
        /// Gets or sets the current OBJECT_SELF value.
        /// </summary>
        uint ObjectSelf { get; set; }

        /// <summary>
        /// Checks if a script exists in the registered script handlers.
        /// </summary>
        /// <param name="scriptName">The name of the script to check</param>
        /// <returns>True if the script exists, false otherwise</returns>
        bool HasScript(string scriptName);

        /// <summary>
        /// Gets the action scripts for the specified script name.
        /// </summary>
        /// <param name="scriptName">The name of the script</param>
        /// <returns>Collection of action delegates and their names</returns>
        IEnumerable<(Action action, string name)> GetActionScripts(string scriptName);

        /// <summary>
        /// Executes an action within the proper script context, managing VM state and OBJECT_SELF.
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="objectId">The object ID to use as OBJECT_SELF during execution</param>
        /// <param name="scriptEventId">The script event ID</param>
        void ExecuteInScriptContext(Action action, uint objectId = 0x7F000000, int scriptEventId = 0);
    }
}