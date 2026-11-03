using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Entry point for the NUI-native conversation system. This service intentionally has no
    /// dependency on Dialog, PlayerDialog, BeginConversation, or generated DLG shells. The logic lives in
    /// <see cref="IConversationService"/>; every member here forwards to the container's instance, and the event
    /// handlers stay here.
    /// </summary>
    public static class Conversation
    {
        private static IConversationService Instance => FacadeAccessor.Resolve<IConversationService>();

        /// <summary>
        /// The registry the conversation graphs run on: the tokens, conditions and actions their text and links name.
        /// </summary>
        public static ConversationRuntime Runtime => Instance.Runtime;

        // Snippet handlers are registered during OnModuleCacheBefore. Graphs must load in the
        // after phase so operation validation never depends on reflection handler ordering within
        // the before phase (Skill.CacheData raises OnSwlorSkillCache re-entrantly while it runs).
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        public static void RegisterGraph(ConversationGraph graph)
        {
            Instance.RegisterGraph(graph);
        }

        public static bool TryGetGraph(string id, out ConversationGraph graph)
        {
            return Instance.TryGetGraph(id, out graph);
        }

        internal static bool IsValidParticipant(uint player)
        {
            return Instance.IsValidParticipant(player);
        }

        /// <summary>
        /// Starts the NUI graph assigned to an object's native Dialog ResRef, when that resource
        /// has been migrated. Returning false leaves the caller free to use an explicit legacy
        /// path for resources listed as migration exceptions.
        /// </summary>
        public static bool TryStartAssigned(uint player, uint owner, uint uiTarget = OBJECT_INVALID)
        {
            return Instance.TryStartAssigned(player, owner, uiTarget);
        }

        /// <summary>
        /// Intercepts native creature dialogue only for migrated resources. A missing graph is an
        /// intentional legacy exception and continues through NWN's normal conversation event.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureConversationBefore)]
        public static void StartAssignedCreatureConversation()
        {
            Instance.StartAssignedCreatureConversation();
        }

        public static void Start(uint player, uint owner, string conversationId, uint uiTarget = OBJECT_INVALID)
        {
            Instance.Start(player, owner, conversationId, uiTarget);
        }

        public static void Start(
            uint player,
            uint owner,
            ConversationGraph graph,
            IConversationRuntime runtime = null,
            uint uiTarget = OBJECT_INVALID)
        {
            Instance.Start(player, owner, graph, runtime, uiTarget);
        }

        /// <summary>Displays any NUI-native conversation session in the shared conversation window.</summary>
        public static void OpenSession(
            uint player,
            uint owner,
            IConversationSession session,
            uint uiTarget = OBJECT_INVALID)
        {
            Instance.OpenSession(player, owner, session, uiTarget);
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void EndOnAreaExit()
        {
            Instance.EndOnAreaExit();
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void EndOnAreaEnter()
        {
            Instance.EndOnAreaEnter();
        }

        public static void End(uint player, uint uiTarget = OBJECT_INVALID)
        {
            Instance.End(player, uiTarget);
        }
    }
}
