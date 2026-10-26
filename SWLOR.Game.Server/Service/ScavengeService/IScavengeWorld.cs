using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>What a session needs to know about the scavenger to carry on: whether they are still there and safe, and how far from the site.</summary>
    /// <param name="IsValid">False once the scavenger has logged out.</param>
    /// <param name="IsDead">True while the scavenger is dead.</param>
    /// <param name="IsInCombat">True while the scavenger is in combat.</param>
    /// <param name="HasWindowOpen">True while the Search window is open on their screen.</param>
    /// <param name="DistanceToSite">How far the scavenger stands from the site, in metres.</param>
    /// <param name="SiteValid">False once the site is gone from the world (removed by a DM or by cleanup).</param>
    public readonly record struct ScavengePlayerState(
        bool IsValid,
        bool IsDead,
        bool IsInCombat,
        bool HasWindowOpen,
        float DistanceToSite,
        bool SiteValid);

    /// <summary>
    /// Everything a scavenge session needs from the game engine. <see cref="ScavengeLifecycle"/> holds the rules and calls only this, so
    /// they run without the engine; <c>NwnScavengeWorld</c> is the engine implementation.
    /// </summary>
    public interface IScavengeWorld
    {
        void Message(uint player, string text);

        /// <summary>A skill-check line shown over the scavenger, such as a Quick Search roll.</summary>
        void SkillCheckMessage(uint player, string text);

        void GiveGatheringXp(uint player, int xp);

        /// <summary>Puts an item in the site's inventory and returns the item's name for the summary.</summary>
        string CreateItem(uint site, string resref, int quantity);

        /// <summary>
        /// Saves what a board in progress has found on the scavenger's record, so a server restart before the visit ends cannot lose it. An
        /// empty list clears it.
        /// </summary>
        void SavePendingFinds(uint player, IReadOnlyList<ScavengeFind> finds);

        /// <summary>
        /// Gives the scavenger whatever is on their record from a visit that never put it in a site, and clears the record in the same
        /// step so it cannot be given twice. Does nothing when the record is empty.
        /// </summary>
        void ReturnPendingFinds(uint player);

        /// <summary>Spawns the site's extra loot tables (<c>LOOT_TABLE_n</c>) for the scavenger, as every successful search always did.</summary>
        void SpawnBonusLoot(uint site, uint player);

        /// <summary>Marks the site as searched (<c>FULLY_HARVESTED</c>), which it is for the rest of its life.</summary>
        void MarkSearched(uint site);

        /// <summary>
        /// Lets the scavenger take what the site gave: after the search animation of the given length, opens the site's inventory for
        /// them. Used when the site holds items.
        /// </summary>
        void OpenSite(uint player, uint site, double animationSeconds);

        /// <summary>
        /// Holds a site that holds items for the scavenger, without opening it for them: nobody else can open it for a short while, and the
        /// scavenger can open it themselves when they come back. Used when the scavenger is away from the site or in a fight.
        /// </summary>
        void HoldSite(uint player, uint site, double animationSeconds);

        /// <summary>Removes a site that gave nothing, after the search animation of the given length.</summary>
        void DestroySite(uint player, uint site, double animationSeconds);

        /// <summary>Opens the Search window. Returns false when it could not be opened.</summary>
        bool OpenWindow(uint player, ScavengeSession session);

        void CloseWindow(uint player);

        ScavengePlayerState GetPlayerState(uint player, uint site);

        /// <summary>The current time, which paces the actions of a board.</summary>
        DateTime UtcNow { get; }

        /// <summary>Runs <paramref name="action"/> after the given seconds, whether or not the scavenger is still in the game.</summary>
        void Schedule(float seconds, Action action);
    }
}
