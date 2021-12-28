using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class MonCalaResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new SpawnTableBuilder();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            CoralIslesInner();
            CoralIslesOuter();

            return _builder.Build();
        }

        private void CoralIslesInner()
        {
            _builder.Create("RESOURCES_MONCALA_CORALISLEINNER")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void CoralIslesOuter()
        {
            _builder.Create("RESOURCES_MONCALA_CORALISLEOUTER")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }
    }
}
