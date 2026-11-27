using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What is left of combat once its parts are split out: the death, exit and six-second handlers, the cleanup that forgets
    /// a creature's combat state in every part, and the refresh of the stat-driven trackers. Static code reaches every other member
    /// on the part that owns it (<c>GameServices</c>).
    /// </summary>
    public interface ICombatService : IStatDrivenTrackerRefresh
    {
        /// <summary>
        /// When a creature dies, applies the killer's and the recent damagers' defeated-enemy effects, then forgets the
        /// defeated creature's combat state.
        /// </summary>
        void ApplyDefeatedEnemyEffects();

        /// <summary>When a creature leaves the module, forgets its combat state.</summary>
        void ClearStatTriggerState();

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        void ClearCombatState();
    }
}
