using System;
using System.Diagnostics;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Processor
{
    public class SpawnProcessor: IEventProcessor
    {
        private readonly AppState _state;
        private readonly IObjectProcessingService _processor;
        private readonly IDataContext _db;
        private readonly ISpawnService _spawn;
        private readonly INWScript _;

        public SpawnProcessor(
            AppState state,
            IObjectProcessingService processor,
            IDataContext db,
            ISpawnService spawn,
            INWScript script)
        {
            _state = state;
            _processor = processor;
            _db = db;
            _spawn = spawn;
            _ = script;
        }

        public void Run(object[] args)
        {
            var spawns = _state.AreaSpawns;
            
            foreach (var spawn in spawns)
            {
                // Check for a valid area - otherwise it causes hangs sometimes when the server shuts down.
                if (!spawn.Key.IsValid) continue;

                AreaSpawn areaSpawn = spawn.Value;

                foreach (var plc in areaSpawn.Placeables.Where(x => x.Respawns || !x.Respawns && !x.HasSpawnedOnce))
                {
                    ProcessSpawn(plc, OBJECT_TYPE_PLACEABLE, spawn.Key);
                }

                foreach (var creature in areaSpawn.Creatures.Where(x => x.Respawns || !x.Respawns && !x.HasSpawnedOnce))
                {
                    ProcessSpawn(creature, OBJECT_TYPE_CREATURE, spawn.Key);
                }
            }
        }


        private void ProcessSpawn(ObjectSpawn spawn, int objectType, NWArea area)
        {
            // Don't process anything that's valid.
            if (spawn.Spawn.IsValid) return;
            spawn.Timer += _processor.ProcessingTickInterval;
            
            // Time to respawn!
            if (spawn.Timer >= spawn.RespawnTime)
            {
                string resref = spawn.Resref;
                Location location = spawn.IsStaticSpawnPoint ? spawn.SpawnLocation : null;
                spawn.HasSpawnedOnce = true;

                if (string.IsNullOrWhiteSpace(resref))
                {
                    var dbSpawn = _db.SpawnObjects.Where(x => x.SpawnID == spawn.SpawnTableID)
                        .OrderBy(o => Guid.NewGuid()).First();
                    resref = dbSpawn.Resref;
                }

                if (location == null)
                {
                    location = _spawn.GetRandomSpawnPoint(area);
                }

                spawn.Spawn = (_.CreateObject(objectType, resref, location));
                if (spawn.NPCGroupID > 0)
                    spawn.Spawn.SetLocalInt("NPC_GROUP", spawn.NPCGroupID);

                if(spawn.DeathVFXID > 0)
                    spawn.Spawn.SetLocalInt("DEATH_VFX", spawn.DeathVFXID);

                if (!string.IsNullOrWhiteSpace(spawn.BehaviourScript) &&
                    string.IsNullOrWhiteSpace(spawn.Spawn.GetLocalString("BEHAVIOUR")))
                    spawn.Spawn.SetLocalString("BEHAVIOUR", spawn.BehaviourScript);

                if (objectType == OBJECT_TYPE_CREATURE)
                    _spawn.AssignScriptEvents(spawn.Spawn.Object);


                if (!string.IsNullOrWhiteSpace(spawn.SpawnRule))
                {
                    App.ResolveByInterface<ISpawnRule>("SpawnRule." + spawn.SpawnRule, rule =>
                    {
                        rule.Run(spawn.Spawn);
                    });
                }

                spawn.Timer = 0.0f;
            }
        }
    }
}
