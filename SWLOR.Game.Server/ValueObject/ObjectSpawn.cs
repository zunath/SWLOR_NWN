using NWN;
using SWLOR.Game.Server.AI;
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
        public NWLocation SpawnLocation { get; set; }
        public bool IsStaticSpawnPoint { get; }
        public ResourceDetails Resource { get; set; }
        public int NPCGroupID { get; set; }
        public string BehaviourScript { get; set; }
        public string SpawnRule { get; set; }
        public int DeathVFXID { get; set; }
        public bool Respawns { get; set; }
        public bool HasSpawnedOnce { get; set; }
        public AIFlags AIFlags { get; set; }

        public NWPlaceable SpawnPlaceable => (Spawn.Object);
        public NWCreature SpawnCreature => (Spawn.Object);
        
        public ObjectSpawn(NWLocation location, bool isStaticSpawnPoint, string resref, float respawnTime = 120.0f)
        {
            Spawn = new NWGameObject();
            SpawnLocation = location;
            IsStaticSpawnPoint = isStaticSpawnPoint;
            RespawnTime = respawnTime;
            Resref = resref;
            Respawns = true;
        }
        public ObjectSpawn(NWLocation location, bool isStaticSpawnPoint, int spawnTableID, float respawnTime = 120.0f)
        {
            Spawn = new NWGameObject();
            SpawnLocation = location;
            IsStaticSpawnPoint = isStaticSpawnPoint;
            RespawnTime = respawnTime;
            SpawnTableID = spawnTableID;
            Respawns = true;
        }
    }
}
