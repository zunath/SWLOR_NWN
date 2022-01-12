using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class HutlarResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new SpawnTableBuilder();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            QionTundra();
            Valley();

            return _builder.Build();
        }

        private void QionTundra()
        {
            _builder.Create("RESOURCES_HUTLAR_QIONTUNDRA")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "ancient_tree")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(5);
        }
        private void Valley()
        {
            _builder.Create("RESOURCES_HUTLAR_VALLEY")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "ancient_tree")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(10);
        }
    }
}
