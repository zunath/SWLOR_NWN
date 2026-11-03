using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// The NUI-native conversation system: the registry of authored graphs, the token, condition and action runtime
    /// they run on, and the start and end of the conversation window. This service has no dependency on Dialog,
    /// PlayerDialog, BeginConversation, or generated DLG shells. The static <c>Service.Conversation</c> forwards to the
    /// container's instance of this interface.
    /// </summary>
    public interface IConversationService
    {
        /// <summary>
        /// The registry the conversation graphs run on: the tokens, conditions and actions their text and links name.
        /// Snippets register their operations here while the module cache is built.
        /// </summary>
        ConversationRuntime Runtime { get; }

        /// <summary>
        /// Loads the embedded authored graphs and the graphs of every graph definition in the loaded assemblies. Snippet
        /// handlers are registered during OnModuleCacheBefore. Graphs must load in the after phase so operation
        /// validation never depends on reflection handler ordering within the before phase.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Validates a graph (its structure and that every condition and action it names is registered) and adds it to
        /// the registry. A graph that fails validation, or an id that is already registered, throws.
        /// </summary>
        void RegisterGraph(ConversationGraph graph);

        /// <summary>Finds a registered graph by its id.</summary>
        bool TryGetGraph(string id, out ConversationGraph graph);

        /// <summary>True for a player, a DM or a possessed creature: the participants a conversation can open for.</summary>
        bool IsValidParticipant(uint player);

        /// <summary>
        /// Starts the NUI graph assigned to an object's native Dialog ResRef, when that resource
        /// has been migrated. Returning false leaves the caller free to use an explicit legacy
        /// path for resources listed as migration exceptions.
        /// </summary>
        bool TryStartAssigned(uint player, uint owner, uint uiTarget = OBJECT_INVALID);

        /// <summary>
        /// Intercepts native creature dialogue only for migrated resources. A missing graph is an
        /// intentional legacy exception and continues through NWN's normal conversation event.
        /// </summary>
        void StartAssignedCreatureConversation();

        /// <summary>Starts a registered graph by id. An unregistered id is reported to the log and the player.</summary>
        void Start(uint player, uint owner, string conversationId, uint uiTarget = OBJECT_INVALID);

        /// <summary>
        /// Starts a graph on a runtime (the service's own when none is given). A failure to start is reported to the log
        /// and the player.
        /// </summary>
        void Start(
            uint player,
            uint owner,
            ConversationGraph graph,
            IConversationRuntime runtime = null,
            uint uiTarget = OBJECT_INVALID);

        /// <summary>Displays any NUI-native conversation session in the shared conversation window.</summary>
        void OpenSession(
            uint player,
            uint owner,
            IConversationSession session,
            uint uiTarget = OBJECT_INVALID);

        /// <summary>Closes the conversation window of the creature that left an area.</summary>
        void EndOnAreaExit();

        /// <summary>Closes the conversation window of the creature that entered an area.</summary>
        void EndOnAreaEnter();

        /// <summary>Closes the conversation window of a participant.</summary>
        void End(uint player, uint uiTarget = OBJECT_INVALID);
    }
}
