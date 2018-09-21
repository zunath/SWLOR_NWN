using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISpawnService
    {
        void OnModuleLoad();
        Location GetRandomSpawnPoint(string areaResref);
        IReadOnlyCollection<ObjectSpawn> GetAreaPlaceableSpawns(string areaResref);
        IReadOnlyCollection<ObjectSpawn> GetAreaCreatureSpawns(string areaResref);
    }
}