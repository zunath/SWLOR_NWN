using System;
using System.Collections.Generic;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.App.Server.Server
{
    /// <summary>
    /// Implementation of IScriptExecutionProvider that bridges the NWScript API
    /// with the SWLOR game server's script execution system.
    /// </summary>
    public class ScriptExecutionProvider : IScriptExecutionProvider
    {
        private readonly IClosureManager _closureManager;
        private readonly ScriptToEventMapper _scriptToEventMapper;
        private readonly IScriptExecutor _scriptExecutor;

        public ScriptExecutionProvider(
            IClosureManager closureManager, 
            ScriptToEventMapper scriptToEventMapper,
            IScriptExecutor scriptExecutor)
        {
            _closureManager = closureManager;
            _scriptToEventMapper = scriptToEventMapper;
            _scriptExecutor = scriptExecutor;
        }

        /// <summary>
        /// Gets or sets the current OBJECT_SELF value via the ClosureManager.
        /// </summary>
        public uint ObjectSelf
        {
            get => _closureManager.ObjectSelf;
            set => _closureManager.ObjectSelf = value;
        }

        /// <summary>
        /// Checks if a script exists in the registered script handlers.
        /// </summary>
        /// <param name="scriptName">The name of the script to check</param>
        /// <returns>True if the script exists, false otherwise</returns>
        public bool HasScript(string scriptName)
        {
            return _scriptToEventMapper.HasEventType(scriptName);
        }

        /// <summary>
        /// Gets the action scripts for the specified script name.
        /// This method is deprecated and returns an empty collection.
        /// All event handling now goes through the IEventAggregator.
        /// </summary>
        /// <param name="scriptName">The name of the script</param>
        /// <returns>Empty collection</returns>
        public IEnumerable<(Action action, string name)> GetActionScripts(string scriptName)
        {
            // No longer used - all event handling goes through IEventAggregator
            return Array.Empty<(Action, string)>();
        }

        /// <summary>
        /// Executes an action within the proper script context, managing VM state and OBJECT_SELF.
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="objectId">The object ID to use as OBJECT_SELF during execution</param>
        /// <param name="scriptEventId">The script event ID</param>
        public void ExecuteInScriptContext(Action action, uint objectId = OBJECT_INVALID, int scriptEventId = 0)
        {
            _scriptExecutor.ExecuteInScriptContext(action, objectId, scriptEventId);
        }
    }
}
