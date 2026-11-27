namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// A combat service that keeps state by creature and forgets it when the creature dies or leaves. <see cref="ICombatService"/>
    /// runs the cleanup over every owner, one <see cref="CombatForgetPhase"/> at a time, so the order of the forgets is the order
    /// of the phases and, inside a phase, the order the owners are registered in.
    /// </summary>
    public interface ICombatCreatureStateOwner
    {
        /// <summary>
        /// Forgets the part of the creature's state that belongs to the phase. An owner ignores the phases it does not own.
        /// </summary>
        /// <param name="creature">The creature that died or left.</param>
        /// <param name="phase">The step of the cleanup being run.</param>
        void ForgetCreatureState(uint creature, CombatForgetPhase phase);
    }
}
