using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// The conversation snippets: the named conditions and actions that conversation graphs and native dialogue
    /// scripts run. The static <c>Service.Snippet</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface ISnippetService
    {
        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache and registered as
        /// operations of the conversation runtime.
        /// </summary>
        void CacheData();

        /// <summary>
        /// When a conversation node with this script assigned in the "Appears When" event is run,
        /// check for any conversation conditions and process them.
        /// </summary>
        bool ConversationAppearsWhen();

        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        void ConversationAction();

        /// <summary>
        /// Evaluates one registered snippet condition without relying on NWScript parameter state.
        /// This is the entry point used by NUI conversations.
        /// </summary>
        bool EvaluateCondition(
            uint player,
            string key,
            IReadOnlyList<string> arguments,
            uint owner = OBJECT_INVALID);

        /// <summary>
        /// Executes one registered snippet action without relying on NWScript parameter state.
        /// </summary>
        bool ExecuteAction(
            uint player,
            string key,
            IReadOnlyList<string> arguments,
            uint owner = OBJECT_INVALID);

        /// <summary>
        /// The object that owns the active conversation. Snippet implementations use this instead
        /// of OBJECT_SELF so they behave identically from native DLG scripts and NUI events.
        /// </summary>
        uint GetExecutionOwner();
    }
}
