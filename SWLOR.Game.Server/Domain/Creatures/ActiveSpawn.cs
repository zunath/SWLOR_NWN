namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>A spawned object that is alive in an area, with the registered spawn that made it. Carried across a hot reload by property name (<see cref="SpawnRegistryService"/>), so the property names stay.</summary>
    public class ActiveSpawn
    {
        public Guid SpawnDetailId { get; set; }
        public uint SpawnObject { get; set; }
        public bool IsRare { get; set; }
    }
}
