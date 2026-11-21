namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What a change to a creature's status effects sets off in the stats (part 2 of the stat split,
    /// Readmes/MaintainabilityRefactorStatSplit.md). Character owns the interface and <c>CreatureStatService</c> implements it
    /// and registers itself with <see cref="IStatusEffectService.RegisterStatEffectListener"/>, so the status effects notify
    /// the stats without a construction cycle: the stats read the status effects for the food check. The status effect split folds this into the
    /// effect lifecycle events.
    /// </summary>
    public interface IStatEffectListener
    {
        /// <summary>A player's status effects were restored after a logout: the food hit points are reconciled.</summary>
        void OnStatusEffectsRestored(uint player);

        /// <summary>A stat-adjusting effect was added, removed or resumed on a creature: its movement rate is recomputed.</summary>
        void OnStatAdjustingEffectsChanged(uint creature);
    }
}
