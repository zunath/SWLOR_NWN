using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>Spawning a property into the game world.</summary>
    public interface IPropertySpawnService
    {
        /// <summary>
        /// Shows a structure's player-given name on its placeable. A structure that still has its catalog name keeps its template's.
        /// </summary>
        void ApplyStructureWorldName(uint placeable, WorldProperty structure);

        /// <summary>
        /// Spawns the property into the game world.
        /// For structures, this means spawning a placeable at the location.
        /// For cities, starships, apartments, and buildings this means spawning area instances.
        /// </summary>
        /// <param name="property">The property to spawn into the world.</param>
        /// <param name="area">The area to spawn the property into. Leave OBJECT_INVALID if spawning an instance.</param>
        void SpawnIntoWorld(WorldProperty property, uint area);
    }
}
