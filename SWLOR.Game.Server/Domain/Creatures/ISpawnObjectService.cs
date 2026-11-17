using SWLOR.Game.Server.Domain.Administration;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The object of a spawn: creating it from a hand-placed serialization or a spawn table, queueing a spawned resource's
    /// despawn, and preparing DM-spawned creatures. The event handler is on <see cref="SpawnObjectService"/>; static callers
    /// reach the container's instance through <c>GameServices.SpawnObject</c>. The DM blueprint catalog reaches the preparation
    /// through <see cref="IDMSpawnPreparation"/>, which Administration owns.
    /// </summary>
    public interface ISpawnObjectService : IDMSpawnPreparation
    {
        /// <summary>
        /// Creates a new spawn object into its spawn area.
        /// Hand-placed objects are deserialized and added to the area.
        /// Spawn tables run their own logic to determine which object to spawn.
        /// </summary>
        /// <param name="spawnId">The ID of the spawn</param>
        /// <param name="detail">The details of the spawn</param>
        SpawnResult SpawnObject(Guid spawnId, SpawnDetail detail);

        /// <summary>
        /// Queues a resource for despawning after the configured amount of time, with a random variance of 25 percent.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="spawnDetailId">The spawn detail ID of the resource</param>
        /// <param name="despawnMinutes">The number of minutes before despawning</param>
        void QueueResourceDespawn(uint resourceObject, Guid spawnDetailId, int despawnMinutes);

        /// <summary>
        /// When a DM spawns a creature, attach all required scripts to it.
        /// </summary>
        void DMSpawnCreature();
    }
}
