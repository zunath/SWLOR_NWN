namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// The preparation every DM-spawned creature receives. Administration owns the interface and Creatures answers it
    /// (<c>DMSpawnPreparation</c>; supply rule 2 of MaintainabilityRefactorDomainOrder.md, P5-15a), so the blueprint catalog
    /// needs nothing from the spawn service.
    /// </summary>
    public interface IDMSpawnPreparation
    {
        /// <summary>
        /// Attaches the scripts and stat adjustments every DM-spawned creature receives.
        /// Used by the native DM creator event and the NUI DM creator.
        /// </summary>
        /// <param name="creature">The creature a DM spawned.</param>
        void PrepareDMSpawnedCreature(uint creature);
    }
}
