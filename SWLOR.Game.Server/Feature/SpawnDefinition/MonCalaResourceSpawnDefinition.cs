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
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "ancient_tree")
                .WithFrequency(70)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_2")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_3")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies2")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "patch_veggies3")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(40);
        }

        private void CoralIslesOuter()
        {
            _builder.Create("RESOURCES_MONCALA_CORALISLEOUTER")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_2")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_3")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies2")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "patch_veggies3")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "ancient_tree")
                .WithFrequency(70)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(40);
        }
    }
}
