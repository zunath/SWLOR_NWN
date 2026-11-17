namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>A spawn waiting for its respawn time. Carried across a hot reload by property name (<see cref="SpawnRegistryService"/>), so the property names stay.</summary>
    public class QueuedSpawn
    {
        public DateTime RespawnTime { get; set; }
        public Guid SpawnDetailId { get; set; }
        public int FailureCount { get; set; } = 0;
    }
}
