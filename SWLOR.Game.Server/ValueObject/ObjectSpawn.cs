using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class ObjectSpawn
    {
        public NWObject Spawn { get; set; }
        public float RespawnTime { get; set; }
        public float Timer { get; set; }
        public int SpawnTableID { get; set; }
        public string Resref { get; set; }
        public Location SpawnLocation { get; }
        public bool IsStaticSpawnPoint { get; }
        public ResourceDetails Resource { get; set; }
        public int NPCGroupID { get; set; }
        public string BehaviourScript { get; set; }
        public string SpawnRule { get; set; }

        public NWPlaceable SpawnPlaceable => (Spawn.Object);
        public NWCreature SpawnCreature => (Spawn.Object);

        public ObjectSpawn(NWObject spawn, bool isStaticSpawnPoint, string resref, float respawnTime = 120.0f)
        {
            Spawn = spawn;
            SpawnLocation = spawn.Location;
            IsStaticSpawnPoint = isStaticSpawnPoint;
            RespawnTime = respawnTime;
            Resref = resref;
        }
        public ObjectSpawn(NWObject spawn, bool isStaticSpawnPoint, int spawnTableID, float respawnTime = 120.0f)
        {
            Spawn = spawn;
            SpawnLocation = spawn.Location;
            IsStaticSpawnPoint = isStaticSpawnPoint;
            RespawnTime = respawnTime;
            SpawnTableID = spawnTableID;
        }
    }
}
