using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Enmity
    {
        // Enemy -> Creature -> EnmityAmount mapping
        private static readonly Dictionary<uint, Dictionary<uint, int>> _enemyEnmityTables = new Dictionary<uint, Dictionary<uint, int>>();

        // Creature -> EnemyList mapping
        private static readonly Dictionary<uint, List<uint>> _creatureToEnemies = new Dictionary<uint, List<uint>>();

        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        [NWNEventHandler("crea_damaged")]
        public static void CreatureDamaged()
        {
            var enemy = OBJECT_SELF;
            var damager = GetLastDamager(enemy);
            var damage = GetTotalDamageDealt();

            ModifyEnmity(damager, enemy, damage);
        }

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        [NWNEventHandler("crea_attacked")]
        public static void CreatureAttacked()
        {
            var enemy = OBJECT_SELF;
            var attacker = GetLastAttacker(enemy);

            ModifyEnmity(attacker, enemy, 1);
        }

        /// <summary>
        /// When a creature dies, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler("crea_death")]
        public static void CreatureDeath()
        {
            var enemy = OBJECT_SELF;
            ClearEnmityTables(enemy);
            RemoveCreatureEnmity(enemy);
        }

        /// <summary>
        /// When a player dies, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler("mod_death")]
        public static void PlayerDeath()
        {
            var player = GetLastPlayerDied();
            RemoveCreatureEnmity(player);
        }

        /// <summary>
        /// When a player leaves, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler("mod_exit")]
        public static void PlayerExit()
        {
            var player = GetExitingObject();
            RemoveCreatureEnmity(player);
        }

        /// <summary>
        /// Retrieves a table containing the creatures on a specific enemy's enmity table.
        /// If no creatures are on the enmity table, an empty dictionary will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to use for retrieval</param>
        /// <returns>A dictionary containing an enemy's enmity table.</returns>
        public static Dictionary<uint, int> GetEnmityTable(uint enemy)
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
        public static uint GetHighestEnmityTarget(uint enemy)
        {
            var enmityTable = GetEnmityTable(enemy);
            var target = enmityTable.Count <= 0 ? OBJECT_INVALID : enmityTable.OrderBy(o => o.Value).First().Key;

            return target;
        }

        /// <summary>
        /// Modifies the enmity of a specific target toward the specific creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        public static void ModifyEnmity(uint creature, uint enemy, int amount)
        {
            // Value is zero, no action necessary.
            if (amount == 0) return;

            // Retrieve the creature's list of associated enemies.
            var enemyList = _creatureToEnemies.ContainsKey(creature) ? _creatureToEnemies[creature] : new List<uint>();

            // Enemy isn't on the creature's list. Add it now.
            if(!enemyList.Contains(enemy))
                enemyList.Add(enemy);

            // Enemy doesn't have any tables yet.
            if (!_enemyEnmityTables.ContainsKey(enemy))
                _enemyEnmityTables[enemy] = new Dictionary<uint, int>();

            // This creature doesn't exist on the enemy's table yet.
            if (!_enemyEnmityTables[enemy].ContainsKey(creature))
                _enemyEnmityTables[enemy][creature] = 0;

            // Modify the enemy's enmity toward this creature.
            var enmityValue = _enemyEnmityTables[enemy][creature] + amount;

            // Enmity cannot fall below 1.
            if (enmityValue < 1)
                enmityValue = 1;

            // Update the enemy's enmity toward this creature.
            _enemyEnmityTables[enemy][creature] = enmityValue;

            // Update this creature's list of enemies.
            _creatureToEnemies[creature] = enemyList;

            // In the event that this enemy does not have a target, immediately start attacking this creature.
            if (GetAttackTarget(enemy) == OBJECT_INVALID)
            {
                AssignCommand(enemy, () => ActionAttack(creature));
            }
        }

        /// <summary>
        /// Modifies the enmity of all creatures who have the specified creature on their enmity table.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="amount">The amount of enmity to adjust by.</param>
        public static void ModifyEnmityOnAll(uint creature, int amount)
        {
            // Value is zero, no action necessary.
            if (amount == 0) return;

            // Creature has no enemies.
            if (!_creatureToEnemies.ContainsKey(creature)) return;

            foreach (var enemy in _creatureToEnemies[creature])
            {
                ModifyEnmity(creature, enemy, amount);
            }
        }

        /// <summary>
        /// Removes a creature from all enmity tables.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        public static void RemoveCreatureEnmity(uint creature)
        {
            // Creature isn't on any enmity table.
            if (!_creatureToEnemies.ContainsKey(creature)) return;

            // Retrieve all of the creatures who have this creature on their enmity table.
            var enemies = _creatureToEnemies[creature];
            foreach (var enemy in enemies)
            {
                _enemyEnmityTables[enemy].Remove(creature);
            }

            // Remove this creature from the targetToCreatures cache.
            _creatureToEnemies.Remove(creature);
        }

        /// <summary>
        /// Clears an enemy's enmity tables and removes associated creatures from cache.
        /// </summary>
        /// <param name="enemy">The enemy whose tables we're clearing</param>
        private static void ClearEnmityTables(uint enemy)
        {
            // Enemy isn't registered as having an enmity table.
            if (!_enemyEnmityTables.ContainsKey(enemy)) return;

            // For every creature on this enemy's enmity table,
            // remove the enemy from that creature's list.
            var creatures = _enemyEnmityTables[enemy];
            foreach (var (creature, _) in creatures)
            {
                _creatureToEnemies[creature].Remove(enemy);
                if (_creatureToEnemies[creature].Count <= 0)
                {
                    _creatureToEnemies.Remove(creature);
                }
            }

            _enemyEnmityTables.Remove(enemy);
        }
    }
}
