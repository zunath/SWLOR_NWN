using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class TatooineSpawnDefinition: ISpawnListDefinition
    {
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder();
            Womprats(builder);
            Sandswimmers(builder);
            Wraid(builder);
            SandDemon(builder);
            TuskenRaider(builder);
            SandWorm(builder);

            return builder.Build();
        }

        private void Womprats(SpawnTableBuilder builder)
        {
            builder.Create("TATOOINE_WOMPRATS", "Womprats")
                .AddSpawn(ObjectType.Creature, "womprat")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void Sandswimmers(SpawnTableBuilder builder)
        {
            builder.Create("TATOOINE_SANDSWIMMERS", "Sandswimmers")
                .AddSpawn(ObjectType.Creature, "sandswimmer")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void Wraid(SpawnTableBuilder builder)
        {
            builder.Create("TATOOINE_WRAID", "Wraid")
                .AddSpawn(ObjectType.Creature, "sandbeetle")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void SandDemon(SpawnTableBuilder builder)
        {
            builder.Create("TATOOINE_SANDDEMON", "Sand Demon")
                .AddSpawn(ObjectType.Creature, "sanddemon")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void TuskenRaider(SpawnTableBuilder builder)
        {
            builder.Create("TATOOINE_TUSKEN_RAIDER", "Tusken Raiders")
                .AddSpawn(ObjectType.Creature, "ext_tusken_tr003")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void SandWorm(SpawnTableBuilder builder)
        {
            builder.Create("TATOOINE_SAND_WORM", "Sand Worm")
                .AddSpawn(ObjectType.Creature, "sandworm")
                .WithFrequency(50)
                .RandomlyWalks();
        }
    }
}
