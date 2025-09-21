using System.Collections.Generic;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Service
{
    public interface IEnmityService
    {
        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        void CreatureDamaged();

        /// <summary>
        /// When a creature is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        void EnemyDamaged();

        /// <summary>
        /// When a creature dies, clear all enmity data for that creature.
        /// </summary>
        void CreatureDied();

        /// <summary>
        /// When an enemy dies, clear all enmity data for that enemy.
        /// </summary>
        void EnemyDied();

        /// <summary>
        /// When a creature leaves the server, clear all enmity data for that creature.
        /// </summary>
        void CreatureLeftServer();

        /// <summary>
        /// When a creature enters an area, clear all enmity data for that creature.
        /// </summary>
        void CreatureEnteredArea();

        /// <summary>
        /// When a creature exits an area, clear all enmity data for that creature.
        /// </summary>
        void CreatureExitedArea();

        /// <summary>
        /// When the module loads, clear all enmity data.
        /// </summary>
        void OnModuleLoad();

        /// <summary>
        /// Adds enmity toward a creature from an enemy.
        /// </summary>
        /// <param name="enemy">The enemy gaining enmity</param>
        /// <param name="creature">The creature the enemy has enmity toward</param>
        /// <param name="amount">The amount of enmity to add</param>
        void AddEnmity(uint enemy, uint creature, int amount);

        /// <summary>
        /// Removes enmity toward a creature from an enemy.
        /// </summary>
        /// <param name="enemy">The enemy losing enmity</param>
        /// <param name="creature">The creature the enemy no longer has enmity toward</param>
        /// <param name="amount">The amount of enmity to remove</param>
        void RemoveEnmity(uint enemy, uint creature, int amount);

        /// <summary>
        /// Clears all enmity data for a creature.
        /// </summary>
        /// <param name="creature">The creature to clear enmity for</param>
        void ClearEnmity(uint creature);

        /// <summary>
        /// Clears all enmity data for an enemy.
        /// </summary>
        /// <param name="enemy">The enemy to clear enmity for</param>
        void ClearEnemyEnmity(uint enemy);

        /// <summary>
        /// Gets all enemies that have enmity toward a creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>A list of enemies with enmity toward the creature</returns>
        List<uint> GetEnemiesWithEnmity(uint creature);

        /// <summary>
        /// Gets all creatures that an enemy has enmity toward.
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        /// <returns>A list of creatures the enemy has enmity toward</returns>
        List<uint> GetCreaturesWithEnmity(uint enemy);

        /// <summary>
        /// Gets the enmity amount toward a creature from an enemy.
        /// </summary>
        /// <param name="enemy">The enemy</param>
        /// <param name="creature">The creature</param>
        /// <returns>The enmity amount</returns>
        int GetEnmityAmount(uint enemy, uint creature);

        /// <summary>
        /// Gets all enmity data for a creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>A dictionary of enemy -> enmity amount</returns>
        Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature);

        /// <summary>
        /// Gets all enmity data for an enemy.
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        /// <returns>A dictionary of creature -> enmity amount</returns>
        Dictionary<uint, int> GetEnmityTowardsAllCreatures(uint enemy);

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity.
        /// If this cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the highest target for.</param>
        /// <returns>The target with the highest enmity</returns>
        uint GetHighestEnmityTarget(uint enemy);

        /// <summary>
        /// Modifies enmity toward a creature from an enemy.
        /// </summary>
        /// <param name="creature">The creature who will have raised enmity toward enemy.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        void ModifyEnmity(uint creature, uint enemy, int amount);

        /// <summary>
        /// Modifies enmity on all enemies that have enmity toward a creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="amount">The amount of enmity to adjust by.</param>
        void ModifyEnmityOnAll(uint creature, int amount);
    }
}
