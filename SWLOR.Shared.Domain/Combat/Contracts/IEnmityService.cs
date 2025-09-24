namespace SWLOR.Shared.Domain.Combat.Contracts
{
    public interface IEnmityService
    {
        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        void CreatureDamaged();

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
        /// Retrieves a table containing the creatures on a specific enemy's enmity table.
        /// If no creatures are on the enmity table, an empty dictionary will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to use for retrieval</param>
        /// <returns>A dictionary containing an enemy's enmity table.</returns>
        Dictionary<uint, int> GetEnmityTable(uint enemy);

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity.
        /// If this cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the highest target for.</param>
        /// <returns>The target with the highest enmity</returns>
        uint GetHighestEnmityTarget(uint enemy);

        /// <summary>
        /// Modifies the enmity of a specific target toward the specific creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        void ModifyEnmity(uint creature, uint enemy, int amount);

        /// <summary>
        /// Modifies the enmity of all creatures who have the specified creature on their enmity table.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="amount">The amount of enmity to adjust by.</param>
        void ModifyEnmityOnAll(uint creature, int amount);

        /// <summary>
        /// Removes a creature from all enmity tables.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        void RemoveCreatureEnmity(uint creature);

        /// <summary>
        /// Determines if a creature has enmity towards any other creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has enmity on any other creature, false otherwise</returns>
        bool HasEnmity(uint creature);

        /// <summary>
        /// Forces a creature to attack the highest enmity target.
        /// If creature does not have enmity, nothing will happen.
        /// If new target is the same as existing, nothing will happen.
        /// </summary>
        void AttackHighestEnmityTarget(uint creature);

        /// <summary>
        /// Retrieves all of the enmity table information for a given creature.
        /// </summary>
        /// <param name="creature">The creature whose tables will be retrieved</param>
        /// <returns>A dictionary of enmity values for a given creature.</returns>
        Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature);
    }
}
