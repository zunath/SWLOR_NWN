namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>A spawned resource waiting to despawn. Carried across a hot reload by property name (<see cref="SpawnRegistryService"/>), so the property names stay.</summary>
    public class ResourceDespawn
    {
        public uint ResourceObject { get; set; }
        public DateTime DespawnTime { get; set; }
        public Guid SpawnDetailId { get; set; }
    }
}
