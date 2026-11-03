using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The conversation snippets: the named conditions and actions that conversation graphs and native dialogue
    /// scripts run. The logic lives in <see cref="ISnippetService"/>; every member here forwards to the container's
    /// instance, and the event handlers stay here.
    /// </summary>
    public static class Snippet
    {
        private static ISnippetService Instance => FacadeAccessor.Resolve<ISnippetService>();

        /// <summary>
        /// When the module loads, all available conversation snippets are loaded into the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// When a conversation node with this script assigned in the "Appears When" event is run,
        /// check for any conversation conditions and process them.
        /// </summary>
        /// <returns></returns>
        [NWNEventHandler(ScriptName.OnDialogAppear)]
        [NWNEventHandler(ScriptName.OnDialogAppears)]
        [NWNEventHandler(ScriptName.OnDialogCondition)]
        [NWNEventHandler(ScriptName.OnDialogConditions)]
        public static bool ConversationAppearsWhen()
        {
            return Instance.ConversationAppearsWhen();
        }

        /// <summary>
        /// When a conversation node with this script assigned in the "Actions Taken" event is run,
        /// check for any conversation actions and process them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogAction)]
        [NWNEventHandler(ScriptName.OnDialogActions)]
        public static void ConversationAction()
        {
            Instance.ConversationAction();
        }

        /// <summary>
        /// Evaluates one registered snippet condition without relying on NWScript parameter state.
        /// This is the entry point used by NUI conversations.
        /// </summary>
        public static bool EvaluateCondition(
            uint player,
            string key,
            IReadOnlyList<string> arguments,
            uint owner = OBJECT_INVALID)
        {
            return Instance.EvaluateCondition(player, key, arguments, owner);
        }

        /// <summary>
        /// Executes one registered snippet action without relying on NWScript parameter state.
        /// </summary>
        public static bool ExecuteAction(
            uint player,
            string key,
            IReadOnlyList<string> arguments,
            uint owner = OBJECT_INVALID)
        {
            return Instance.ExecuteAction(player, key, arguments, owner);
        }

        /// <summary>
        /// The object that owns the active conversation. Snippet implementations use this instead
        /// of OBJECT_SELF so they behave identically from native DLG scripts and NUI events.
        /// </summary>
        public static uint GetExecutionOwner()
        {
            return Instance.GetExecutionOwner();
        }
    }
}
