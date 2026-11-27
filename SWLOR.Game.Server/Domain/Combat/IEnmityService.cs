using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Who each enemy hates: the enmity tables and their reads, threat checks, removal and clearing, and the attack-command memory.
    /// The table part takes no service. <see cref="IEnmityTargetQuery.GetHighestEnmityTarget"/> is Character's question. The changes
    /// that feed the tables are on <see cref="IEnmityChangeService"/> and the choice and command of an attack target on
    /// <see cref="IEnmityAttackService"/>; no part holds a table, every access goes through these members.
    /// </summary>
    public interface IEnmityService : IEnmityTargetQuery, IEspionageEnmityQuery
    {
        /// <summary>
        /// Retrieves a table containing the creatures on a specific enemy's enmity table.
        /// If no creatures are on the enmity table, an empty dictionary will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to use for retrieval</param>
        /// <returns>A dictionary containing an enemy's enmity table.</returns>
        Dictionary<uint, int> GetEnmityTable(uint enemy);

        /// <summary>
        /// Determines whether a creature holds the enemy's attention by a safe margin: it must be the
        /// enemy's highest enmity target and lead the next-highest creature by at least
        /// <see cref="EnmityService.SecureThreatLeadPercent"/>. Taunts are wasted when this is already true.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table is checked.</param>
        /// <param name="creature">The creature expected to hold the enemy's attention.</param>
        /// <returns>true if the creature's threat lead is secure, false otherwise.</returns>
        bool IsThreatSecured(uint enemy, uint creature);

        /// <summary>Whether a holder's enmity leads the next-highest enmity by the secure margin.</summary>
        bool IsThreatLeadSecure(int holderEnmity, int nextHighestEnmity);

        /// <summary>The enmity left after a percentage of the current amount is removed, rounded up and never below one.</summary>
        int CalculateReducedEnmity(int current, int percent);

        /// <summary>
        /// Removes a creature from one enemy's enmity table without making the enemy pick a new target.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        /// <param name="enemy">The enemy whose enmity table is updated.</param>
        void RemoveEnmity(uint creature, uint enemy);

        /// <summary>
        /// Removes the tracked proximity enmity contribution from a specific enemy's table.
        /// </summary>
        /// <param name="creature">The creature to remove proximity enmity for.</param>
        /// <param name="enemy">The enemy whose enmity table should be updated.</param>
        /// <returns>true if proximity enmity was removed.</returns>
        bool RemoveProximityEnmity(uint creature, uint enemy);

        /// <summary>Whether proximity enmity is tracked for the pair.</summary>
        bool HasProximityEnmity(uint creature, uint enemy);

        /// <summary>
        /// Whether the pair's whole enmity came from the enemy's aggro aura.
        /// </summary>
        bool HasOnlyProximityEnmity(uint creature, uint enemy);

        /// <summary>
        /// Clears every creature from an enemy's enmity table.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table will be cleared.</param>
        void ClearEnmityTable(uint enemy);

        /// <summary>
        /// Clears an enemy's enmity tables and removes associated creatures from cache.
        /// </summary>
        /// <param name="enemy">The enemy whose tables we're clearing</param>
        void ClearEnmityTables(uint enemy);

        /// <summary>
        /// Determines if a creature has enmity towards any other creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has enmity on any other creature, false otherwise</returns>
        bool HasEnmity(uint creature);

        /// <summary>
        /// Retrieves all of the enmity table information for a given creature.
        /// </summary>
        /// <param name="creature">The creature whose tables will be retrieved</param>
        /// <returns>A dictionary of enmity values for a given creature.</returns>
        Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature);

        /// <summary>The creature's enmity on the enemy's table, or zero when it is not on it.</summary>
        int GetRawEnmityAmount(uint creature, uint enemy);

        /// <summary>The part of the creature's enmity on the enemy's table that came from the aggro aura, or zero.</summary>
        int GetProximityEnmityAmount(uint creature, uint enemy);

        /// <summary>The enemies that have the creature on their tables, as a copy (empty when there are none).</summary>
        uint[] GetEnemiesOf(uint creature);

        /// <summary>
        /// Puts the creature on the enemy's table when it is not there and adds the amount to its enmity (never below one),
        /// and lists the enemy among the creature's enemies.
        /// </summary>
        /// <param name="creature">The creature whose enmity is raised.</param>
        /// <param name="enemy">The enemy whose table is written.</param>
        /// <param name="amount">The adjusted amount to add.</param>
        void AddEnmity(uint creature, uint enemy, int amount);

        /// <summary>
        /// Reduces the creature's enmity on the enemy's table by a percentage, and its tracked proximity share with it.
        /// </summary>
        /// <returns>true when the creature was on the table and its enmity changed.</returns>
        bool ReduceEnmityEntry(uint creature, uint enemy, int percent);

        /// <summary>Adds an applied amount to the tracked proximity share of the creature's enmity on the enemy's table.</summary>
        void AddProximityEnmity(uint creature, uint enemy, int appliedAmount);

        /// <summary>Removes the creature's entry from the enemy's table and the enemy from the creature's list.</summary>
        void RemoveEnmityTableEntry(uint creature, uint enemy);

        /// <summary>Forgets the tracked proximity share of the creature's enmity on the enemy's table.</summary>
        void RemoveProximityEnmityTracking(uint creature, uint enemy);

        /// <summary>
        /// The attack command last issued to a creature: when, and against whom.
        /// </summary>
        /// <returns>true when a command is remembered for the creature.</returns>
        bool TryGetAttackCommand(uint creature, out DateTime issuedAt, out uint target);

        /// <summary>Remembers the attack command issued to a creature.</summary>
        void RecordAttackCommand(uint creature, uint target, DateTime issuedAt);

        /// <summary>Forgets the attack command remembered for a creature.</summary>
        void ForgetAttackCommand(uint creature);
    }
}
