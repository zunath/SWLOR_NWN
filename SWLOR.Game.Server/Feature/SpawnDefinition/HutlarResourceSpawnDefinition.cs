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
            Wastes();
            FrozenCave();

            return _builder.Build();
        }

        private void QionTundra()
        {
            _builder.Create("RESOURCES_HUTLAR_QIONTUNDRA")
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "ancient_tree")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "aracia_tree")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "patch_veggies3")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5);
        }
        private void Valley()
        {
            _builder.Create("RESOURCES_HUTLAR_VALLEY")
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "ancient_tree")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "aracia_tree")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies3")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(2)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(10);
        }

        private void Wastes()
        {
            _builder.Create("RESOURCES_HUTLAR_WASTES")
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "ancient_tree")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "aracia_tree")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "patch_veggies3")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5);
        }

        private void FrozenCave()
        {
            _builder.Create("RESOURCES_FROZEN_CAVE")
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(20)
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "patch_veggies3")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(2)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(40)
                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(10);

        }
    }
}
