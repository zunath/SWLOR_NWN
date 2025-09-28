using System.Numerics;

namespace SWLOR.Component.World.Contracts
{
    /// <summary>
    /// Service responsible for creating and configuring spawn objects.
    /// </summary>
    public interface ISpawnCreationService
    {
        /// <summary>
        /// Creates a spawn object at the specified location.
        /// </summary>
        /// <param name="spawnId">The spawn ID</param>
        /// <param name="serializedObject">The serialized object data (for hand-placed spawns)</param>
        /// <param name="spawnTableId">The spawn table ID (for table-based spawns)</param>
        /// <param name="area">The area to spawn in</param>
        /// <param name="position">The position to spawn at</param>
        /// <param name="facing">The facing direction</param>
        /// <param name="useRandomSpawnLocation">Whether to use random spawn location</param>
        /// <returns>The created spawn object, or OBJECT_INVALID if creation failed</returns>
        uint CreateSpawnObject(
            string spawnId,
            string serializedObject,
            string spawnTableId,
            uint area,
            Vector3 position,
            float facing,
            bool useRandomSpawnLocation);

        /// <summary>
        /// Adjusts scripts on a spawned object.
        /// </summary>
        /// <param name="spawn">The spawn object to adjust</param>
        void AdjustScripts(uint spawn);

        /// <summary>
        /// Adjusts stats on a spawned object.
        /// </summary>
        /// <param name="spawn">The spawn object to adjust</param>
        void AdjustStats(uint spawn);

        /// <summary>
        /// Handles DM spawn creature events.
        /// </summary>
        void DMSpawnCreature();
    }
}
