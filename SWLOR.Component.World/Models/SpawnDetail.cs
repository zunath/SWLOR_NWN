using System.Numerics;

namespace SWLOR.Component.World.Models
{
    /// <summary>
    /// Represents details about a spawn point.
    /// </summary>
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
        public bool UseRandomSpawnLocation { get; set; }
    }
}
