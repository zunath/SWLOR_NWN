using System;
using System.Collections.Generic;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Implementation of IScriptExecutionProvider that bridges the NWScript API
    /// with the SWLOR game server's script execution system.
    /// </summary>
    public class ScriptExecutionProviderImpl : IScriptExecutionProvider
    {
        /// <summary>
        /// Gets or sets the current OBJECT_SELF value via the ClosureManager.
        /// </summary>
        public uint ObjectSelf
        {
            get => ServerManager.Bootstrapper.ClosureManager.ObjectSelf;
            set => ServerManager.Bootstrapper.ClosureManager.ObjectSelf = value;
        }

        /// <summary>
        /// Checks if a script exists in the registered script handlers.
        /// </summary>
        /// <param name="scriptName">The name of the script to check</param>
        /// <returns>True if the script exists, false otherwise</returns>
        public bool HasScript(string scriptName)
        {
            return ServerManager.Scripts.HasScript(scriptName);
        }

        /// <summary>
        /// Gets the action scripts for the specified script name.
        /// </summary>
        /// <param name="scriptName">The name of the script</param>
        /// <returns>Collection of action delegates and their names</returns>
        public IEnumerable<(Action action, string name)> GetActionScripts(string scriptName)
        {
            return ServerManager.Scripts.GetActionScripts(scriptName);
        }

        /// <summary>
        /// Executes an action within the proper script context, managing VM state and OBJECT_SELF.
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="objectId">The object ID to use as OBJECT_SELF during execution</param>
        /// <param name="scriptEventId">The script event ID</param>
        public void ExecuteInScriptContext(Action action, uint objectId = OBJECT_INVALID, int scriptEventId = 0)
        {
            ServerManager.Executor.ExecuteInScriptContext(action, objectId, scriptEventId);
        }
    }
}