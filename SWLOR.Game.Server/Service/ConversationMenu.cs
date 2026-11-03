using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Registry and entry point for code-driven NUI conversations. This is independent of the old
    /// Dialog service and never allocates or starts a generated DLG shell. The logic lives in
    /// <see cref="IConversationMenuService"/>; every member here forwards to the container's instance, and the event
    /// handlers stay here.
    /// </summary>
    public static class ConversationMenu
    {
        private static IConversationMenuService Instance => FacadeAccessor.Resolve<IConversationMenuService>();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        public static bool Contains(string name) => Instance.Contains(name);

        /// <summary>
        /// Compatibility entry point for module objects whose event script already points at the
        /// SWLOR conversation router. The destination is graph data or a code menu, never a shell.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void StartFromObjectEvent()
        {
            Instance.StartFromObjectEvent();
        }

        [NWNEventHandler(ScriptName.OnDialogStartConversation)]
        public static void StartFromLocalConversation()
        {
            Instance.StartFromLocalConversation();
        }

        public static bool TryStart(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID)
        {
            return Instance.TryStart(player, owner, name, uiTarget);
        }

        public static void Start(uint player, uint owner, string name, uint uiTarget = OBJECT_INVALID)
        {
            Instance.Start(player, owner, name, uiTarget);
        }
    }
}
