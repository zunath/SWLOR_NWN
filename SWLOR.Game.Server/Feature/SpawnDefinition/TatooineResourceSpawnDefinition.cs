using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class TatooineResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            AridHillyDesert();
            TatooineResources();

            return _builder.Build();
        }

        private void AridHillyDesert()
        {
            _builder.Create("TATOOINE_RESOURCE_ARID_HILLY_DESERT")
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(5);
        }

        private void TatooineResources()
        {
            _builder.Create("TATOOINE_RESOURCE_GENERAL")
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(10);
        }
    }
}
