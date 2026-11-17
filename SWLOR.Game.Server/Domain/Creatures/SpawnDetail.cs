namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>One registered spawn: a hand-placed creature the boot captured or a spawn table slot of an area. Carried across a hot reload by property name (<see cref="SpawnRegistryService"/>), so the property names stay.</summary>
    public class SpawnDetail
    {
        public string SerializedObject { get; set; }
        public string SpawnTableId { get; set; }
        public uint Area { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Facing { get; set; }
        public int RespawnDelayMinutes { get; set; }
        public int RespawnDelayMaximumMinutes { get; set; }
        public bool UseRandomSpawnLocation { get; set; }
    }
}
