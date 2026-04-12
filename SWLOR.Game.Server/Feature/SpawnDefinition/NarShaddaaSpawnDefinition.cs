using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class NarShaddaaSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            RedBladeGang();
            BlackSerpents();
            HiddenBlades();
            Troublemakers();
            ArenaFighters();
            SniperSpawn();
            SerpentLeader();
            RogueDroid();
            PirateOutpost();
            PirateCommandDroid();
            ScavengerDroids();
            ThiefSpawns();
            SlaverCaptain();
            GreatArkanianDragon();

            return _builder.Build();
        }

        private void RedBladeGang()
        {
            _builder.Create("NAR_RED_BLADES")
                .AddSpawn(ObjectType.Creature, "nar_redblade")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void BlackSerpents()
        {
            _builder.Create("NAR_BLACK_SERPENTS")
                .AddSpawn(ObjectType.Creature, "nar_serpent")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void HiddenBlades()
        {
            _builder.Create("NAR_HIDDEN_BLADES")
                .AddSpawn(ObjectType.Creature, "nar_hiddenblade")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void Troublemakers()
        {
            _builder.Create("NAR_TROUBLEMAKERS")
                .AddSpawn(ObjectType.Creature, "nar_troublemaker")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void ArenaFighters()
        {
            _builder.Create("NAR_ARENA_FIGHTERS")
                .AddSpawn(ObjectType.Creature, "nar_arenafight")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void SniperSpawn()
        {
            _builder.Create("NAR_SNIPER")
                .AddSpawn(ObjectType.Creature, "nar_sniper")
                .WithFrequency(1)
                .ReturnsHome();
        }

        private void SerpentLeader()
        {
            _builder.Create("NAR_SERPENT_LEADER")
                .AddSpawn(ObjectType.Creature, "nar_serp_leader")
                .WithFrequency(1)
                .ReturnsHome();
        }

        private void RogueDroid()
        {
            _builder.Create("NAR_ROGUE_DROID")
                .AddSpawn(ObjectType.Creature, "nar_rogue_droid")
                .WithFrequency(50)
                .ReturnsHome();
        }

        private void PirateOutpost()
        {
            _builder.Create("NAR_PIRATE")
                .AddSpawn(ObjectType.Creature, "nar_pirate")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();

            _builder.Create("NAR_PIRATES")
                .AddSpawn(ObjectType.Creature, "nar_pirate")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void PirateCommandDroid()
        {
            _builder.Create("NAR_COMMAND_DROID")
                .AddSpawn(ObjectType.Creature, "nar_cmd_droid")
                .WithFrequency(1)
                .ReturnsHome();
        }

        private void ScavengerDroids()
        {
            _builder.Create("NAR_SCAVENGERS")
                .AddSpawn(ObjectType.Creature, "nar_scavenger")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void ThiefSpawns()
        {
            _builder.Create("NAR_THIEVES")
                .AddSpawn(ObjectType.Creature, "nar_thief")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void SlaverCaptain()
        {
            _builder.Create("NAR_SLAVER_CAPTAIN")
                .AddSpawn(ObjectType.Creature, "nar_slavercaptn")
                .WithFrequency(1)
                .ReturnsHome();
        }

        private void GreatArkanianDragon()
        {
            _builder.Create("NAR_GREAT_ARKANIAN_DRAGON")
                .AddSpawn(ObjectType.Creature, "garkaniandragon")
                .WithFrequency(1)
                .ReturnsHome();
        }
    }
}

