namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What stealth asks of the enmity code when a creature hides or shows itself: have the enemies that hate it choose their
    /// attack target again. Combat owns the interface; the attack part (<see cref="IEnmityAttackService"/>) implements it and
    /// registers with <see cref="IStealthService.RegisterRetargetListener"/> at boot, so stealth notifies the enmity code without a
    /// construction cycle (the attack part reads stealth for hidden targets). It is separate from <see cref="IStealthAggroListener"/>,
    /// which the creature AI implements.
    /// </summary>
    public interface IStealthRetargetListener
    {
        /// <summary>
        /// Makes every enemy with the creature on its enmity table choose its attack target again
        /// (see <see cref="IEnmityAttackService.ReevaluateEnemyAttackTargets"/>).
        /// </summary>
        void ReevaluateEnemyAttackTargets(uint creature);

        /// <summary>
        /// Forces a creature to attack the highest enmity target it can see (see <see cref="IEnmityAttackService.AttackHighestEnmityTarget"/>).
        /// </summary>
        void AttackHighestEnmityTarget(uint creature);
    }
}
