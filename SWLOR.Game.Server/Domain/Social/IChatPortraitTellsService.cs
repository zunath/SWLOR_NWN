using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Keeps a tell started by clicking a chat-log portrait from reaching a player other than the one whose line
    /// carried that portrait. Static callers reach the container's instance
    /// of this interface through <c>GameServices.ChatPortraitTells</c>; the event handlers are on <see cref="ChatPortraitTellsService"/>.
    /// </summary>
    public interface IChatPortraitTellsService
    {
        /// <summary>
        /// When a player enters, forgets what the login object's previous client saw and remembers the character.
        /// </summary>
        void RememberEnteringPlayer();

        /// <summary>The work of <see cref="RememberEnteringPlayer"/> for one player (a hot reload's restore step).</summary>
        void RememberPlayer(uint player);

        /// <summary>
        /// When a player leaves, forgets what their client saw and the character behind the login object.
        /// </summary>
        void ForgetExitingPlayer();

        /// <summary>
        /// Records a chat line the engine is sending to one client. Called from a native hook, so this only
        /// reads engine and managed state.
        /// </summary>
        /// <param name="recipientSlot">The receiving client's player ID.</param>
        /// <param name="speaker">The object that spoke the line.</param>
        void RecordChatLine(uint recipientSlot, uint speaker);

        /// <summary>How many chat lines from other players have reached the player's client this session.</summary>
        long CountLinesReceived(uint player);

        /// <summary>
        /// When a player clicks a chat-log portrait, warns them if the portrait may now point to someone else.
        /// </summary>
        void CheckPortraitClick();

        /// <summary>
        /// Classifies a chat-log portrait click by the clicking player and the player the client resolved it to.
        /// </summary>
        ChatPortraitClick ClickPortrait(uint player, uint portraitOwner);

        /// <summary>
        /// Whether to refuse a tell because a portrait click that may belong to another player started it, telling
        /// the sender why. A tell from the Player List names its target exactly and is never refused.
        /// </summary>
        /// <param name="confirmedTarget">True when the tell goes to the player the sender confirmed with a second portrait click.</param>
        bool RefuseStalePortraitTell(uint sender, uint target, bool isDirectTell, out bool confirmedTarget);

        /// <summary>
        /// Classifies a chat-log portrait the way a click on it would, without counting as a click.
        /// </summary>
        ChatPortraitClick ClassifyPortrait(uint player, uint portraitOwner);
    }
}
