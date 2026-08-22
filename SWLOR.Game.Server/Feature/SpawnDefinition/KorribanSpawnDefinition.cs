using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class KorribanSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            Wastelands();
            Valley();
            Ravine();
            Caverns();
            Dunes();
            SithCrypt();
            SithTemples();
            FrogBoss();
            KorribanFortress();
            KorribanForgeCaverns();
            KorribanSithCryptDepths();

            KorforgeRareElites();
            KorcryptRareElites();
            return _builder.Build();
        }

        private void KorcryptRareElites()
        {
            _builder.Create("KORRIBAN_SITH_CRYPT_DEPTHS_RARES", "Korriban Sith Crypt Depths - Rare Elites")
                .AddSpawn(ObjectType.Creature, "cryptwarden").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "markahunger").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "eclipseshade").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void KorforgeRareElites()
        {
            _builder.Create("KORRIBAN_FORGE_CAVERNS_RARES", "Korriban Forge Caverns - Rare Elites")
                .AddSpawn(ObjectType.Creature, "forgewright").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "flameweaver").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "banecaller").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void Wastelands()
        {
            _builder.Create("KORRIBAN_WASTELANDS", "Wastelands")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();

        }
        private void Valley()
        {
            _builder.Create("KORRIBAN_VALLEY", "Valley")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Ravine()
        {
            _builder.Create("KORRIBAN_RAVINE", "Ravine")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Caverns()
        {
            _builder.Create("KORRIBAN_CAVERNS", "Caverns")
                .AddSpawn(ObjectType.Creature, "shyrack")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Dunes()
        {
            _builder.Create("KORRIBAN_DUNES", "Dunes")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_wraid")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void SithCrypt()
        {
            _builder.Create("KORRIBAN_SITH_CRYPT", "Sith Crypt")
                .AddSpawn(ObjectType.Creature, "s_app")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "s_app_m")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_frostbind")
                .WithFrequency(15)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void KorribanFortress()
        {
            _builder.Create("KorribanDungeonMaster")
                .AddSpawn(ObjectType.Creature, "vkorrdun4boss")
                .WithFrequency(1)
                .RespawnDelay(120);

            _builder.Create("KorribanDungeonGuardian")
                .AddSpawn(ObjectType.Creature, "vkorrdungate")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonCouncilGuard")
                .AddSpawn(ObjectType.Creature, "vkorrduncouncilg")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonMarauder")
                .AddSpawn(ObjectType.Creature, "vkorrdunmarauder")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5)

                .AddSpawn(ObjectType.Creature, "vkorrdunsorc")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5);

            _builder.Create("KorribanDungeonTrooper")
                .AddSpawn(ObjectType.Creature, "vkorrdun1rifle")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(2)

                .AddSpawn(ObjectType.Creature, "vkorrdun1sword")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(2);

            _builder.Create("KorribanDungeonWarform")
                .AddSpawn(ObjectType.Creature, "vkorrdunwarform")
                .WithFrequency(2)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonInquisitor")
                .AddSpawn(ObjectType.Creature, "vkorrduninquis")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonIndustrial")
                .AddSpawn(ObjectType.Creature, "vkorrdundroidhvy")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(10)

                .AddSpawn(ObjectType.Creature, "vkorrdunmarauder")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5)

                .AddSpawn(ObjectType.Creature, "vkorrdunsorc")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5);
        }
        private void SithTemples()
        {
            _builder.Create("KORRIBAN_TEMPLES")
                .AddSpawn(ObjectType.Creature, "s_app")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "s_app_m")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "shyrack")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "tukata")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korriinitiate")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_frostbind")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_wraid")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void FrogBoss()
        {
            _builder.Create("FrogBoss", "Alchemized Frog Boss")
                .AddSpawn(ObjectType.Creature, "frogboss")
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(120);
        }

        private void KorribanForgeCaverns()
        {
            _builder.Create("CAPSTONE_KORRIBAN_FORGE_CAVERNS", "Korriban Forge Caverns - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_absdef_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_absdef_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_absdef_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_soulasc_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_soulasc_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_soulasc_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_forcebane_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_forcebane_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_forcebane_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void KorribanSithCryptDepths()
        {
            _builder.Create("CAPSTONE_KORRIBAN_SITH_CRYPT_DEPTHS", "Korriban Sith Crypt Depths - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_lightstand_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_lightstand_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_lightstand_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_darkhung_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_darkhung_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_darkhung_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_eclipse_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_eclipse_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_eclipse_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
