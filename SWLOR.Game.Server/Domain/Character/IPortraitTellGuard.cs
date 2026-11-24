namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The refusal of a tell that a chat-log portrait click may have sent to the wrong player. Character owns the interface and
    /// <c>IChatPortraitTellsService</c> extends it, so the tell handler needs nothing from Social.
    /// </summary>
    public interface IPortraitTellGuard
    {
        /// <summary>
        /// Whether to refuse a tell because a portrait click that may belong to another player started it, telling
        /// the sender why. A tell from the Player List names its target exactly and is never refused.
        /// </summary>
        /// <param name="confirmedTarget">True when the tell goes to the player the sender confirmed with a second portrait click.</param>
        bool RefuseStalePortraitTell(uint sender, uint target, bool isDirectTell, out bool confirmedTarget);
    }
}
