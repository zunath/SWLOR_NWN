namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The changes to enmity and the events that cause them: damage, healing and attacks raise it, taunts catch it up, reductions
    /// and proximity move it, and death, exit and limbo remove it. The tables are on <see cref="IEnmityService"/>; the change part
    /// asks <see cref="IEnmityAttackService"/> for a new attack target after each change. Static callers reach it through
    /// <c>GameServices.EnmityChange</c>.
    /// </summary>
    public interface IEnmityChangeService
    {
        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        void CreatureDamaged();

        /// <summary>The enmity a damage amount adds, scaled by the damager's adjustment and never below one.</summary>
        int CalculateDamageEnmity(int damage, int adjustment);

        /// <summary>Starts watching a heal so its effective amount can be credited when it completes.</summary>
        void HealingStarted();

        /// <summary>Raises the healer's enmity on every enemy of the healed creature by half of what the heal restored.</summary>
        void HealingCompleted();

        /// <summary>The enmity a heal adds: half of the hit points that really landed, without nested healing.</summary>
        int CalculateHealingEnmity(int requested, int previousHP, int currentHP, long nestedHealing = 0);

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        void CreatureAttacked();

        /// <summary>
        /// When a creature dies, remove all enmity tables it is associated with.
        /// </summary>
        void CreatureDeath();

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        void CreatureDestroyed();

        /// <summary>
        /// When a player dies, remove them from all enmity tables.
        /// </summary>
        void PlayerDeath();

        /// <summary>
        /// When a player leaves, remove them from all enmity tables.
        /// </summary>
        void PlayerExit();

        /// <summary>
        /// When a DM limbos creatures, ensure their enmity is wiped.
        /// </summary>
        void CreatureLimbo();

        /// <summary>
        /// Modifies the enmity of a specific target toward the specific creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        void ModifyEnmity(uint creature, uint enemy, int amount);

        /// <summary>The amount of a gain after the percent adjustment; reductions are not adjusted.</summary>
        int CalculateAdjustedEnmity(int amount, int adjustment);

        /// <summary>
        /// Catches up to an eligible rival plus a fixed, stat-adjusted lead. Repeated taunts
        /// while already ahead do not accumulate threat; there is no target lock.
        /// </summary>
        bool TryTaunt(uint creature, uint enemy, int bonus);

        /// <summary>The enmity a taunt sets: the creature's own enmity, or the rival's plus the lead, whichever is more.</summary>
        int CalculateTauntEnmity(int current, int rival, int adjustedBonus);

        /// <summary>Keeps an enmity percent adjustment within the minimum and maximum.</summary>
        int ClampEnmityPercentAdjustment(int adjustment);

        /// <summary>
        /// Modifies the enmity of all creatures who have the specified creature on their enmity table.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="amount">The amount of enmity to adjust by.</param>
        void ModifyEnmityOnAll(uint creature, int amount);

        /// <summary>
        /// Reduces the creature's current enmity on every enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        void ReduceEnmityOnAll(uint creature, int percent);

        /// <summary>
        /// Reduces the creature's current enmity on a single enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="enemy">The enemy whose enmity table will be adjusted.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        void ReduceEnmity(uint creature, uint enemy, int percent);

        /// <summary>
        /// Modifies enmity from aggro proximity and tracks the resulting contribution separately.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The proximity enmity amount to apply.</param>
        void ModifyProximityEnmity(uint creature, uint enemy, int amount);

        /// <summary>
        /// Removes a creature from all enmity tables.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        void RemoveCreatureEnmity(uint creature);
    }
}
