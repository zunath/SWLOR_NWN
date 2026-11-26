namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What the combat leash asks of the enmity code: have a creature choose its attack target again. Combat owns the interface; the
    /// attack part (<see cref="IEnmityAttackService"/>) implements it and registers with
    /// <see cref="ICombatLeash.RegisterRetargetListener"/> at boot, so the leash notifies the enmity code without a construction
    /// cycle (the attack part calls the leash).
    /// </summary>
    public interface ILeashRetargetListener
    {
        /// <summary>
        /// Forces a creature to attack the highest enmity target it can see (see <see cref="IEnmityAttackService.AttackHighestEnmityTarget"/>).
        /// </summary>
        void AttackHighestEnmityTarget(uint creature);
    }
}
