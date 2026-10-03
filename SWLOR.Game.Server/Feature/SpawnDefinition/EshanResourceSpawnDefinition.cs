using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class EshanResourceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            SilverwoodExpanse();
            Battlegrounds();
            Shattervein();
            Shimmerdeep();

            return _builder.Build();
        }

        private void SilverwoodExpanse()
        {
            AddHighLevelTrees("ESHAN_SILVERWOOD_EXPANSE_RESOURCES", "Eshan - Silverwood Expanse");
        }

        private void Battlegrounds()
        {
            AddHighLevelTrees("ESHAN_BATTLEGROUNDS_RESOURCES", "Eshan - Battlegrounds");
        }

        private void Shattervein()
        {
            AddHighLevelOres("ESHAN_SHATTERVEIN_RESOURCES", "Eshan - The Shattervein");
        }

        private void Shimmerdeep()
        {
            AddHighLevelOres("ESHAN_SHIMMERDEEP_RESOURCES", "Eshan - The Shimmerdeep");
        }

        private void AddHighLevelTrees(string tableId, string name)
        {
            _builder.Create(tableId, name)
                .ResourceDespawnDelay(120)
                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "aracia_tree")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "hyphae_tree")
                .WithFrequency(5);
        }

        private void AddHighLevelOres(string tableId, string name)
        {
            _builder.Create(tableId, name)
                .ResourceDespawnDelay(90)
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1);
        }
    }
}
