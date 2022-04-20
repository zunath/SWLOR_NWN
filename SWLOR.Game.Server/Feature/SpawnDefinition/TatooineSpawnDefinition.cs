using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class TatooineSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new SpawnTableBuilder();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            Womprats();
            Sandswimmers();
            Wraid();
            SandDemon();
            TuskenRaider();
            SandWorm();
            AridHillyDesert();
            Flatlands();
            NorthernDunes();

            return _builder.Build();
        }

        private void Womprats()
        {
            _builder.Create("TATOOINE_WOMPRATS", "Womprats")
                .AddSpawn(ObjectType.Creature, "womprat")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void Sandswimmers()
        {
            _builder.Create("TATOOINE_SANDSWIMMERS", "Sandswimmers")
                .AddSpawn(ObjectType.Creature, "sandswimmer")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void Wraid()
        {
            _builder.Create("TATOOINE_WRAID", "Wraid")
                .AddSpawn(ObjectType.Creature, "sandbeetle")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void SandDemon()
        {
            _builder.Create("TATOOINE_SANDDEMON", "Sand Demon")
                .AddSpawn(ObjectType.Creature, "sanddemon")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void TuskenRaider()
        {
            _builder.Create("TATOOINE_TUSKEN_RAIDER", "Tusken Raiders")
                .AddSpawn(ObjectType.Creature, "ext_tusken_tr003")
                .WithFrequency(50)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "tusken_melee")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void SandWorm()
        {
            _builder.Create("TATOOINE_SAND_WORM", "Sand Worm")
                .AddSpawn(ObjectType.Creature, "sandworm")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void AridHillyDesert()
        {
            _builder.Create("TATOOINE_ARID_HILLY_DESERT", "Arid Hilly Desert")
                .AddSpawn(ObjectType.Creature, "ext_tusken_tr003")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "womprat")
                .WithFrequency(40)
                .RandomlyWalks();
        }

        private void Flatlands()
        {
            _builder.Create("TATOOINE_FLATLANDS", "Flatlands")
                .AddSpawn(ObjectType.Creature, "sandswimmer")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "sandworm")
                .WithFrequency(2)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "sanddemon")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "sandbeetle")
                .WithFrequency(40)
                .RandomlyWalks();
        }

        private void NorthernDunes()
        {
            _builder.Create("TATOOINE_NORTHERNDUNES", "Northern Dunes")
                .AddSpawn(ObjectType.Creature, "sandswimmer")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "sanddemon")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "sandbeetle")
                .WithFrequency(40)
                .RandomlyWalks();
        }
    }
}
