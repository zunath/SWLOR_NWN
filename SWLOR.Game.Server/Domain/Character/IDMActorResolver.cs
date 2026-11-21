namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The DM avatar behind a creature a DM possesses, which the disguise and the item trash rules read. Character owns the
    /// interface and <c>IDMPresenceService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md).
    /// </summary>
    public interface IDMActorResolver
    {
        /// <summary>
        /// Returns the DM avatar behind a creature a DM possesses, or the object itself.
        /// DM windows opened while possessing a creature belong to that creature's view.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        uint GetDM(uint actor);
    }
}
