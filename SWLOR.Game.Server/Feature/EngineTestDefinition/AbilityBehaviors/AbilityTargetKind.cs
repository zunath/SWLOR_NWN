namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
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
        HostileCreature = 1
    }
}
