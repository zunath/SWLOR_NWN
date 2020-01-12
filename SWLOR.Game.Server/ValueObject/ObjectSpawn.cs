using NWN;
using SWLOR.Game.Server.AI;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.ValueObject
{
    public class ObjectSpawn
    {
        public NWObject Spawn { get; set; }
        public float RespawnTime { get; set; }
        public float Timer { get; set; }
        public Spawn SpawnTableID { get; set; }
        public string Resref { get; set; }
        public NWLocation SpawnLocation { get; set; }
        public bool IsStaticSpawnPoint { get; }
        public ResourceDetails Resource { get; set; }
        public NPCGroup NPCGroupID { get; set; }
        public string BehaviourScript { get; set; }
        public string SpawnRule { get; set; }
        public Vfx DeathVFXID { get; set; }
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
        public ObjectSpawn(NWLocation location, bool isStaticSpawnPoint, Spawn spawnTableID, float respawnTime = 120.0f)
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
