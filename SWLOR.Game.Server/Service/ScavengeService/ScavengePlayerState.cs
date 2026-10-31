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
}
