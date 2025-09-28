using System;

namespace SWLOR.Component.World.Models
{
    /// <summary>
    /// Represents a queued spawn waiting to be created.
    /// </summary>
    public class QueuedSpawn
    {
        public DateTime RespawnTime { get; set; }
        public Guid SpawnDetailId { get; set; }
        public int FailureCount { get; set; } = 0;
    }
}
