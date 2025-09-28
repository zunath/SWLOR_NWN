using System;

namespace SWLOR.Component.World.Models
{
    /// <summary>
    /// Represents an active spawn in the game world.
    /// </summary>
    public class ActiveSpawn
    {
        public Guid SpawnDetailId { get; set; }
        public uint SpawnObject { get; set; }
    }
}
