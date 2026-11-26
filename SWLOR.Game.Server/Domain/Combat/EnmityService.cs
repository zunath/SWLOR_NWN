using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Who each enemy hates: the enmity tables, the aggro-proximity share of them, the attack-command memory, the reads and threat
    /// checks over them, and their removal and clearing. The logic that used to be static in <c>Service.Enmity</c> is moved here
    /// unchanged; the tables are this instance's own fields and are read per damage event and per attack without being rebuilt. The
    /// change part (<see cref="EnmityChangeService"/>) and the attack part (<see cref="EnmityAttackService"/>) reach the tables only
    /// through the members of <see cref="IEnmityService"/>; static callers reach this instance through <c>GameServices.Enmity</c>.
    /// </summary>
    public class EnmityService : IEnmityService
    {
        public const int SecureThreatLeadPercent = 25;
        // Enemy -> Creature -> EnmityAmount mapping
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<uint, Dictionary<uint, int>> _enemyEnmityTables = new();

        // Creature -> EnemyList mapping
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<uint, List<uint>> _creatureToEnemies = new();

        // Enemy -> Creature -> proximity enmity contribution mapping
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<uint, Dictionary<uint, int>> _proximityEnmityAmounts = new();

        // The attack command last issued to each creature. The tables clear it with the creature, so it lives with them.
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, DateTime> _attackCommandTimes = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, uint> _attackCommandTargets = new();

        /// <summary>
        /// Retrieves a table containing the creatures on a specific enemy's enmity table.
        /// If no creatures are on the enmity table, an empty dictionary will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to use for retrieval</param>
        /// <returns>A dictionary containing an enemy's enmity table.</returns>
        public Dictionary<uint, int> GetEnmityTable(uint enemy)
        {
            if(!_enemyEnmityTables.ContainsKey(enemy))
                return new Dictionary<uint, int>();

            return _enemyEnmityTables[enemy].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity.
        /// If this cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the highest target for.</param>
        /// <returns>The target with the highest enmity</returns>
        public uint GetHighestEnmityTarget(uint enemy)
        {
            var enmityTable = GetEnmityTable(enemy);
            var target = enmityTable.Count <= 0
                ? OBJECT_INVALID
                : enmityTable.MaxBy(o => o.Value).Key;

            return target;
        }

        /// <summary>
        /// Determines whether a creature holds the enemy's attention by a safe margin: it must be the
        /// enemy's highest enmity target and lead the next-highest creature by at least
        /// <see cref="SecureThreatLeadPercent"/>. Taunts are wasted when this is already true.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table is checked.</param>
        /// <param name="creature">The creature expected to hold the enemy's attention.</param>
        /// <returns>true if the creature's threat lead is secure, false otherwise.</returns>
        public bool IsThreatSecured(uint enemy, uint creature)
        {
            if (!_enemyEnmityTables.TryGetValue(enemy, out var table) ||
                !table.TryGetValue(creature, out var creatureEnmity))
            {
                return false;
            }

            var nextHighest = 0;
            foreach (var (other, amount) in table)
            {
                if (other != creature && amount > nextHighest)
                    nextHighest = amount;
            }

            return IsThreatLeadSecure(creatureEnmity, nextHighest);
        }

        public bool IsThreatLeadSecure(int holderEnmity, int nextHighestEnmity)
        {
            if (holderEnmity <= 0 || holderEnmity < nextHighestEnmity)
                return false;

            return (long)holderEnmity * 100 >= (long)nextHighestEnmity * (100 + SecureThreatLeadPercent);
        }

        public int CalculateReducedEnmity(int current, int percent)
        {
            var reduction = ((long)Math.Max(0, current) * Math.Clamp(percent, 0, 100) + 99) / 100;
            return (int)Math.Max(1, current - reduction);
        }

        /// <summary>
        /// Removes a creature from one enemy's enmity table without making the enemy pick a new target.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        /// <param name="enemy">The enemy whose enmity table is updated.</param>
        public void RemoveEnmity(uint creature, uint enemy)
        {
            RemoveEnmityTableEntry(creature, enemy);
            RemoveProximityEnmityTracking(creature, enemy);
        }

        /// <summary>
        /// Removes the tracked proximity enmity contribution from a specific enemy's table.
        /// </summary>
        /// <param name="creature">The creature to remove proximity enmity for.</param>
        /// <param name="enemy">The enemy whose enmity table should be updated.</param>
        /// <returns>true if proximity enmity was removed.</returns>
        public bool RemoveProximityEnmity(uint creature, uint enemy)
        {
            if (!_proximityEnmityAmounts.TryGetValue(enemy, out var proximityTable) ||
                !proximityTable.TryGetValue(creature, out var proximityAmount))
            {
                return false;
            }

            if (!_enemyEnmityTables.TryGetValue(enemy, out var table) ||
                !table.TryGetValue(creature, out var amount))
            {
                RemoveProximityEnmityTracking(creature, enemy);
                return false;
            }

            if (amount <= proximityAmount)
            {
                RemoveEnmityTableEntry(creature, enemy);
            }
            else
            {
                table[creature] = amount - proximityAmount;
            }

            RemoveProximityEnmityTracking(creature, enemy);
            return true;
        }

        public bool HasProximityEnmity(uint creature, uint enemy)
        {
            return _proximityEnmityAmounts.TryGetValue(enemy, out var table) &&
                   table.ContainsKey(creature);
        }

        /// <summary>
        /// Returns true when a specific creature/enemy pair has enmity beyond the amount created
        /// solely by the enemy's aggro aura.
        /// </summary>
        public bool HasNonProximityEnmity(uint creature, uint enemy)
        {
            return GetRawEnmityAmount(creature, enemy) > 0 &&
                   !HasOnlyProximityEnmity(creature, enemy);
        }

        /// <summary>
        /// Returns true when an enemy has any enmity that did not come solely from its aggro aura.
        /// Attack, damage, and ability enmity make the enemy an active combatant and therefore an
        /// invalid source for a new Espionage infiltration attempt.
        /// </summary>
        public bool HasNonProximityEnmity(uint enemy)
        {
            if (!_enemyEnmityTables.TryGetValue(enemy, out var table))
                return false;

            return table.Keys.Any(creature => HasNonProximityEnmity(creature, enemy));
        }

        /// <summary>
        /// Returns true when a creature appears on any enemy table for more than aggro proximity.
        /// This distinguishes real combat from the proximity-only entries created as multiple
        /// stealthed players cross overlapping aggro auras.
        /// </summary>
        public bool HasNonProximityEnmityForCreature(uint creature)
        {
            if (!_creatureToEnemies.TryGetValue(creature, out var enemies))
                return false;

            return enemies.Any(enemy => HasNonProximityEnmity(creature, enemy));
        }

        /// <summary>
        /// Returns true when either member of a creature/enemy pair has combat enmity involving
        /// someone outside that pair. Pair-specific checks use this to distinguish an expected
        /// aggro transition from unrelated combat.
        /// </summary>
        public bool HasNonProximityEnmityOutsidePair(uint first, uint second)
        {
            return HasNonProximityEnmityAsCreatureOutsidePair(first, second) ||
                   HasNonProximityEnmityAsEnemyOutsidePair(first, second) ||
                   HasNonProximityEnmityAsCreatureOutsidePair(second, first) ||
                   HasNonProximityEnmityAsEnemyOutsidePair(second, first);
        }

        private bool HasNonProximityEnmityAsCreatureOutsidePair(uint creature, uint pairedEnemy)
        {
            return _creatureToEnemies.TryGetValue(creature, out var enemies) &&
                   enemies.Any(enemy =>
                       enemy != pairedEnemy && HasNonProximityEnmity(creature, enemy));
        }

        private bool HasNonProximityEnmityAsEnemyOutsidePair(uint enemy, uint pairedCreature)
        {
            return _enemyEnmityTables.TryGetValue(enemy, out var table) &&
                   table.Keys.Any(creature =>
                       creature != pairedCreature && HasNonProximityEnmity(creature, enemy));
        }

        /// <summary>
        /// Clears every creature from an enemy's enmity table.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table will be cleared.</param>
        public void ClearEnmityTable(uint enemy)
        {
            ClearEnmityTables(enemy);
        }

        /// <summary>
        /// Clears an enemy's enmity tables and removes associated creatures from cache.
        /// </summary>
        /// <param name="enemy">The enemy whose tables we're clearing</param>
        public void ClearEnmityTables(uint enemy)
        {
            _attackCommandTimes.Remove(enemy);
            _attackCommandTargets.Remove(enemy);

            // Enemy isn't registered as having an enmity table.
            if (!_enemyEnmityTables.ContainsKey(enemy))
            {
                _proximityEnmityAmounts.Remove(enemy);
                return;
            }

            // For every creature on this enemy's enmity table,
            // remove the enemy from that creature's list.
            var creatures = _enemyEnmityTables[enemy];
            foreach (var (creature, _) in creatures)
            {
                if (!_creatureToEnemies.TryGetValue(creature, out var enemies))
                    continue;

                enemies.Remove(enemy);
                if (enemies.Count <= 0)
                {
                    _creatureToEnemies.Remove(creature);
                }
            }

            _enemyEnmityTables.Remove(enemy);
            _proximityEnmityAmounts.Remove(enemy);
        }

        /// <summary>
        /// Determines if a creature has enmity towards any other creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has enmity on any other creature, false otherwise</returns>
        public bool HasEnmity(uint creature)
        {
            return _creatureToEnemies.ContainsKey(creature)
                   && _creatureToEnemies[creature].Count > 0;
        }

        /// <summary>
        /// Retrieves all of the enmity table information for a given creature.
        /// </summary>
        /// <param name="creature">The creature whose tables will be retrieved</param>
        /// <returns>A dictionary of enmity values for a given creature.</returns>
        public Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature)
        {
            var enemyList = _creatureToEnemies.ContainsKey(creature)
                ? _creatureToEnemies[creature]
                : new List<uint>();

            var result = new Dictionary<uint, int>();

            foreach (var enemy in enemyList)
            {
                if(!_enemyEnmityTables.ContainsKey(enemy) ||
                   !_enemyEnmityTables[enemy].ContainsKey(creature))
                    continue;

                var enmity = _enemyEnmityTables[enemy][creature];

                result.Add(enemy, enmity);
            }

            return result;
        }

        public bool HasOnlyProximityEnmity(uint creature, uint enemy)
        {
            var rawAmount = GetRawEnmityAmount(creature, enemy);
            if (rawAmount <= 0)
                return false;

            var proximityAmount = GetProximityEnmityAmount(creature, enemy);
            return proximityAmount >= rawAmount;
        }

        public int GetRawEnmityAmount(uint creature, uint enemy)
        {
            return _enemyEnmityTables.TryGetValue(enemy, out var table) &&
                   table.TryGetValue(creature, out var amount)
                ? amount
                : 0;
        }

        public int GetProximityEnmityAmount(uint creature, uint enemy)
        {
            return _proximityEnmityAmounts.TryGetValue(enemy, out var table) &&
                   table.TryGetValue(creature, out var amount)
                ? amount
                : 0;
        }

        public uint[] GetEnemiesOf(uint creature)
        {
            return _creatureToEnemies.TryGetValue(creature, out var enemies)
                ? enemies.ToArray()
                : Array.Empty<uint>();
        }

        public void AddEnmity(uint creature, uint enemy, int amount)
        {
            // Retrieve the creature's list of associated enemies.
            var enemyList = _creatureToEnemies.ContainsKey(creature) ? _creatureToEnemies[creature] : new List<uint>();

            // Enemy isn't on the creature's list. Add it now.
            if (!enemyList.Contains(enemy))
                enemyList.Add(enemy);

            // Enemy doesn't have any tables yet.
            if (!_enemyEnmityTables.ContainsKey(enemy))
                _enemyEnmityTables[enemy] = new Dictionary<uint, int>();

            // This creature doesn't exist on the enemy's table yet.
            if (!_enemyEnmityTables[enemy].ContainsKey(creature))
                _enemyEnmityTables[enemy][creature] = 0;

            // Modify the enemy's enmity toward this creature.
            var enmityValue = (int)Math.Clamp((long)_enemyEnmityTables[enemy][creature] + amount, 1, int.MaxValue);

            // Enmity cannot fall below 1.
            if (enmityValue < 1)
                enmityValue = 1;

            // Update the enemy's enmity toward this creature.
            _enemyEnmityTables[enemy][creature] = enmityValue;

            // Update this creature's list of enemies.
            _creatureToEnemies[creature] = enemyList;
        }

        public bool ReduceEnmityEntry(uint creature, uint enemy, int percent)
        {
            if (!_enemyEnmityTables.TryGetValue(enemy, out var table) ||
                !table.TryGetValue(creature, out var currentEnmity))
            {
                return false;
            }

            table[creature] = CalculateReducedEnmity(currentEnmity, percent);
            if (_proximityEnmityAmounts.TryGetValue(enemy, out var proximity) && proximity.TryGetValue(creature, out var amount))
                proximity[creature] = Math.Min(table[creature], CalculateReducedEnmity(amount, percent));

            return true;
        }

        public void AddProximityEnmity(uint creature, uint enemy, int appliedAmount)
        {
            if (!_proximityEnmityAmounts.TryGetValue(enemy, out var table))
            {
                table = new Dictionary<uint, int>();
                _proximityEnmityAmounts[enemy] = table;
            }

            table.TryGetValue(creature, out var existingAmount);
            table[creature] = existingAmount + appliedAmount;
        }

        public void RemoveEnmityTableEntry(uint creature, uint enemy)
        {
            if (_enemyEnmityTables.TryGetValue(enemy, out var table))
            {
                table.Remove(creature);
                if (table.Count <= 0)
                {
                    _enemyEnmityTables.Remove(enemy);
                }
            }

            if (!_creatureToEnemies.TryGetValue(creature, out var enemies))
                return;

            enemies.Remove(enemy);
            if (enemies.Count <= 0)
            {
                _creatureToEnemies.Remove(creature);
            }
        }

        public void RemoveProximityEnmityTracking(uint creature, uint enemy)
        {
            if (!_proximityEnmityAmounts.TryGetValue(enemy, out var table))
                return;

            table.Remove(creature);
            if (table.Count <= 0)
            {
                _proximityEnmityAmounts.Remove(enemy);
            }
        }

        public bool TryGetAttackCommand(uint creature, out DateTime issuedAt, out uint target)
        {
            var hasTime = _attackCommandTimes.TryGetValue(creature, out issuedAt);
            var hasTarget = _attackCommandTargets.TryGetValue(creature, out target);
            return hasTime || hasTarget;
        }

        public void RecordAttackCommand(uint creature, uint target, DateTime issuedAt)
        {
            _attackCommandTimes[creature] = issuedAt;
            _attackCommandTargets[creature] = target;
        }

        public void ForgetAttackCommand(uint creature)
        {
            _attackCommandTimes.Remove(creature);
            _attackCommandTargets.Remove(creature);
        }
    }
}
