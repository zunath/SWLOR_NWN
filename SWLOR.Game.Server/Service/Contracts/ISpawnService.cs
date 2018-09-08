using System.Collections.Generic;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISpawnService
    {
        void OnModuleLoad();
        IReadOnlyCollection<ObjectSpawn> GetAreaPlaceableSpawns(string areaResref);
        IReadOnlyCollection<ObjectSpawn> GetAreaCreatureSpawns(string areaResref);
    }
}