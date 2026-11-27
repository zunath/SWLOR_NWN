namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Choosing and commanding the attack target: the visible creature with the most enmity, the command that sends the enemy at it,
    /// and the retarget the leash and stealth ask for. The tables are on <see cref="IEnmityService"/>, the changes to them on
    /// <see cref="IEnmityChangeService"/>. The attack part is the listener of both retarget notices
    /// (<see cref="ILeashRetargetListener"/>, <see cref="IStealthRetargetListener"/>); static callers reach it through
    /// <c>GameServices.EnmityAttack</c>.
    /// </summary>
    public interface IEnmityAttackService
    {
        /// <summary>
        /// Gives the leash and stealth this service as the listener of their retarget notices. A reload runs boot handlers that
        /// rerun before it adopts the carried state, so both lists are full when the first notice fires.
        /// </summary>
        void RegisterRetargetListeners();

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity that the enemy can attack.
        /// Creatures hidden from the enemy by invisibility or undetected stealth keep their enmity but are skipped until
        /// they can be seen again. If no creature can be attacked, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the attack target for.</param>
        /// <returns>The visible target with the highest enmity</returns>
        uint GetHighestEnmityAttackTarget(uint enemy);

        /// <summary>
        /// Whether the enemy can attack the creature now: it is valid, alive, in the enemy's area and not hidden from it.
        /// </summary>
        bool IsEligibleAttackTarget(uint enemy, uint creature);

        /// <summary>
        /// Makes every enemy with this creature on its enmity table choose its attack target again.
        /// Call after the creature becomes hidden so enemies react without waiting for their next
        /// heartbeat or combat round.
        /// </summary>
        /// <param name="creature">The creature whose enemies should re-evaluate their target.</param>
        void ReevaluateEnemyAttackTargets(uint creature);

        /// <summary>
        /// Resumes existing combat after the caller clears the action queue. The previous attack
        /// command is no longer pending, so its throttle must not delay the replacement command.
        /// </summary>
        void ResumeAttackAfterActionsCleared(uint creature);

        /// <summary>
        /// Forces a creature to attack the highest enmity target it can see.
        /// Stops an existing chase when its last proximity target is no longer in range.
        /// Stops attacking, but keeps its enmity, while every target is hidden by invisibility or stealth.
        /// If creature did not have enmity, nothing will happen.
        /// If the creature is already actively attacking that target, nothing will happen.
        /// </summary>
        void AttackHighestEnmityTarget(uint creature);

        /// <summary>Queues the attack command that sends a creature at a target, moving into range first when it is too far.</summary>
        void IssueAttackCommand(uint creature, uint target, bool clearActions = true);
    }
}
