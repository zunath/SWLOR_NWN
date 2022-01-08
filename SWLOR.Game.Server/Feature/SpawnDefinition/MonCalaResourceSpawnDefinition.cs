using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class MonCalaResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            CoralIslesInner();
            CoralIslesOuter();

            return _builder.Build();
        }

        private void CoralIslesInner()
        {
            _builder.Create("RESOURCES_MONCALA_CORALISLEINNER")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(70)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_1")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(40);
        }

        private void CoralIslesOuter()
        {
            _builder.Create("RESOURCES_MONCALA_CORALISLEOUTER")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(70)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_1")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(40);
        }
    }
}
