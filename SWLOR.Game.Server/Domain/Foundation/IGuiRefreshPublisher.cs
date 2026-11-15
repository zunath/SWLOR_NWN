using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Announces that something a window shows changed, so the windows that show it refresh. Foundation owns the
    /// interface and the window registry (<c>IGuiService</c>) implements it, so the domains below the UI announce changes
    /// without naming it (supply rule 3 of MaintainabilityRefactorDomainOrder.md: a notice, never a question). A call
    /// costs what the direct call cost.
    /// </summary>
    public interface IGuiRefreshPublisher
    {
        /// <summary>
        /// Publishes a refresh event. All subscribed view models will be refreshed for this particular player.
        /// This can be useful for pushing updates to a window based on external circumstances.
        /// For example: Refreshing the XP bar when a player gains XP.
        /// If the player does not have the window open on-screen, nothing will happen.
        /// </summary>
        /// <param name="player">The player to refresh.</param>
        /// <param name="payload">The refresh payload.</param>
        /// <param name="uiTarget">Where the windows are shown, when that is not the player, such as a creature a DM possesses.</param>
        void PublishRefreshEvent<T>(uint player, T payload, uint uiTarget = OBJECT_INVALID)
            where T : IGuiRefreshEvent;

        /// <summary>
        /// Refreshes every open character sheet currently displaying the supplied target.
        /// This includes sheets opened by another player, such as DM-inspected creatures.
        /// </summary>
        void PublishCharacterSheetRefreshEvent<T>(uint target, T payload)
            where T : IGuiRefreshEvent;
    }
}
