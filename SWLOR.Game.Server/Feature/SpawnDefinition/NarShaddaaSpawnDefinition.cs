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
            Pirates();
            PirateCommandDroid();
            ScavengerDroids();
            ThiefSpawns();
            SlaverCaptain();
            GreatArkanianDragon();
            DragonLoot();
            SmugglersMoonFightClubBackrooms();
            CzerkaArmsTestRange();
            CzerkaArmsRareElites();
            FightClubRareElites();

            return _builder.Build();
        }

        private void FightClubRareElites()
        {
            _builder.Create("FIGHTCLUB_BACKROOMS_RARES", "Fight Club Backrooms - Rare Elites")
                .AddSpawn(ObjectType.Creature, "ironjaw").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "quickdraw").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "hexcaller").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        // Dedicated rare table (tagged waypoint in pw_ar_czarmrange) so the capstone lesson table
        // stays exactly the general enemy steps.
        private void CzerkaArmsRareElites()
        {
            _builder.Create("CZERKA_ARMS_TEST_RANGE_RARES", "Czerka Arms Test Range - Rare Elites")
                .AddSpawn(ObjectType.Creature, "overwatch")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "blastbreaker")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "suppressor")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
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

        private void Pirates()
        {
            _builder.Create("NAR_PIRATE")
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
                .RandomlyWalks()
                .ReturnsHome();
        }
        private void DragonLoot()
        {
            _builder.Create("DRAGON_LOOT")
                .AddSpawn(ObjectType.Placeable, "dragon_loot")
                .WithFrequency(1)
                .ReturnsHome();
        }

        private void SmugglersMoonFightClubBackrooms()
        {
            _builder.Create("CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS", "Smuggler's Moon Fight Club Backrooms - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_cripdef_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_cripdef_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_cripdef_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_tempbloom_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_tempbloom_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_tempbloom_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_redbloom_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_redbloom_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_redbloom_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void CzerkaArmsTestRange()
        {
            _builder.Create("CAPSTONE_CZERKA_ARMS_TEST_RANGE", "Czerka Arms Test Range - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_killbox_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_killbox_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_killbox_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_oneshot_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_oneshot_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_oneshot_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_rainsteel_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_rainsteel_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_rainsteel_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
