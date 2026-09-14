namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public enum AbilityTargetKind
    {
        /// <summary>
        /// The ability is used by the caster on itself.
        /// </summary>
        Self = 0,

        /// <summary>
        /// The ability is used on a freshly spawned Hostile-faction creature standing near the caster.
        /// </summary>
        HostileCreature = 1,

        /// <summary>
        /// The ability is used on a freshly spawned same-faction ally standing near the caster
        /// (for friendly-target abilities that disallow self-targeting).
        /// </summary>
        FriendlyCreature = 2
    }
}
