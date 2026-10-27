using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.ChatPortraitTellService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Keeps a tell started by clicking a chat-log portrait from reaching a player other than the one whose line
    /// carried that portrait. See <see cref="ChatPortraitTellGuard"/> for why the client can address someone else.
    /// The logic and state live in <see cref="IChatPortraitTellsService"/>; every member here forwards to the
    /// container's instance, and the event handlers stay here so their names in the handler order manifest do not change.
    /// </summary>
    public static class ChatPortraitTells
    {
        private static IChatPortraitTellsService Instance => FacadeAccessor.Resolve<IChatPortraitTellsService>();

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RememberEnteringPlayer()
        {
            Instance.RememberEnteringPlayer();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetExitingPlayer()
        {
            Instance.ForgetExitingPlayer();
        }

        /// <summary>
        /// Records a chat line the engine is sending to one client. Called from a native hook, so this only
        /// reads engine and managed state.
        /// </summary>
        /// <param name="recipientSlot">The receiving client's player ID.</param>
        /// <param name="speaker">The object that spoke the line.</param>
        internal static void RecordChatLine(uint recipientSlot, uint speaker)
        {
            Instance.RecordChatLine(recipientSlot, speaker);
        }

        /// <summary>How many chat lines from other players have reached the player's client this session.</summary>
        public static long CountLinesReceived(uint player)
        {
            return Instance.CountLinesReceived(player);
        }

        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
        public static void CheckPortraitClick()
        {
            Instance.CheckPortraitClick();
        }

        /// <summary>
        /// Classifies a chat-log portrait click by the clicking player and the player the client resolved it to.
        /// </summary>
        public static ChatPortraitClick ClickPortrait(uint player, uint portraitOwner)
        {
            return Instance.ClickPortrait(player, portraitOwner);
        }

        /// <summary>
        /// Whether to refuse a tell because a portrait click that may belong to another player started it, telling
        /// the sender why. A tell from the Player List names its target exactly and is never refused.
        /// </summary>
        public static bool RefuseStalePortraitTell(uint sender, uint target, bool isDirectTell)
        {
            return Instance.RefuseStalePortraitTell(sender, target, isDirectTell);
        }
    }
}
