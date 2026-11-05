namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Registry and entry point for code-driven NUI conversations. This is independent of the old Dialog service and
    /// never allocates or starts a generated DLG shell. The static <c>Service.ConversationMenu</c> forwards to the
    /// container's instance of this interface.
    /// </summary>
    public interface IConversationMenuService
    {
        /// <summary>Registers every code-driven conversation menu definition of the loaded assemblies by its type name.</summary>
        void CacheData();

        /// <summary>True when a menu is registered under the name.</summary>
        bool Contains(string name);

        /// <summary>
        /// Compatibility entry point for module objects whose event script already points at the
        /// SWLOR conversation router. The destination is graph data or a code menu, never a shell.
        /// </summary>
        void StartFromObjectEvent();

        /// <summary>Opens the conversation graph or code menu named by the CONVERSATION local of the running object.</summary>
        void StartFromLocalConversation();

        /// <summary>Starts a registered menu, or returns false when no menu has that name.</summary>
        bool TryStart(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID);

        /// <summary>Starts a registered menu. An unregistered name or a menu that fails to build is reported to the log and the player.</summary>
        void Start(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID);
    }
}
