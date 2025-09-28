using System;

namespace SWLOR.Component.World.Models
{
    /// <summary>
    /// Represents a resource scheduled for despawning.
    /// </summary>
    public class ResourceDespawn
    {
        public uint ResourceObject { get; set; }
        public DateTime DespawnTime { get; set; }
        public Guid SpawnDetailId { get; set; }
    }
}
