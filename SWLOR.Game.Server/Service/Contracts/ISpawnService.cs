using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISpawnService
    {
        void OnModuleLoad();
        Location GetRandomSpawnPoint(NWArea area);
        IReadOnlyCollection<ObjectSpawn> GetAreaPlaceableSpawns(NWArea area);
        IReadOnlyCollection<ObjectSpawn> GetAreaCreatureSpawns(NWArea area);
        void AssignScriptEvents(NWCreature creature);
        void InitializeAreaSpawns(NWArea area);
    }
}