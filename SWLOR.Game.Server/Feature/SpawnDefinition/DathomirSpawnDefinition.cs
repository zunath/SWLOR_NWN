using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class DathomirSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            CaveRuins();
            Desert();
            DesertWestSide();
            GrottoCaverns();
            Grottos();
            GrottosBoss();
            MountainCaves();
            Mountains();
            RuinBase();
            TarnishedJungles();
            TarnishedJunglesNorth();
            TribeVillage();
            HiddenTunnel();
            DathomirTarnJunglePreserve();
            DathomirGrottoApexDen();

            DathomirGrottoRareElites();
            DathtarnRareElites();
            return _builder.Build();
        }

        private void DathtarnRareElites()
        {
            _builder.Create("DATHOMIR_TARN_JUNGLE_PRESERVE_RARES", "Dathomir Tarn Jungle Preserve - Rare Elites")
                .AddSpawn(ObjectType.Creature, "tarnapexmaw").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "quillstalker").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "rhydelalpha").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void DathomirGrottoRareElites()
        {
            _builder.Create("DATHOMIR_GROTTO_APEX_DEN_RARES", "Dathomir Grotto Apex Den - Rare Elites")
                .AddSpawn(ObjectType.Creature, "grottoalpha").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "spinequill").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "ritestalker").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void CaveRuins()
        {
            _builder.Create("DATHOMIR_CAVE_RUINS")
                .AddSpawn(ObjectType.Creature, "vdathguard")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathshaman")
                .RandomlyWalks()
                .WithFrequency(20)

                .AddSpawn(ObjectType.Creature, "vdathtribal")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathturtle")
                .RandomlyWalks()
                .WithFrequency(1)

                .AddSpawn(ObjectType.Creature, "vdathpurbole")
                .RandomlyWalks()
                .WithFrequency(80);
        }

        private void Desert()
        {
            _builder.Create("DATHOMIR_DESERT")
                .AddSpawn(ObjectType.Creature, "vdathguard")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathshaman")
                .RandomlyWalks()
                .WithFrequency(20)

                .AddSpawn(ObjectType.Creature, "vdathtribal")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathturtle")
                .RandomlyWalks()
                .WithFrequency(5);
        }

        private void DesertWestSide()
        {
            _builder.Create("DATHOMIR_DESERT_WEST_SIDE")
                .AddSpawn(ObjectType.Creature, "vdathguard")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathshaman")
                .RandomlyWalks()
                .WithFrequency(20)

                .AddSpawn(ObjectType.Creature, "vdathtribal")
                .RandomlyWalks()
                .WithFrequency(100);
        }

        private void GrottoCaverns()
        {
            _builder.Create("DATHOMIR_GROTTO_CAVERNS")
                .AddSpawn(ObjectType.Creature, "vdathturtle")
                .RandomlyWalks()
                .WithFrequency(5)

                .AddSpawn(ObjectType.Creature, "vdathssurian")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathpurbole")
                .RandomlyWalks()
                .WithFrequency(100);
        }

        private void Grottos()
        {
            _builder.Create("DATHOMIR_GROTTOS")
                .AddSpawn(ObjectType.Creature, "vdathssurian")
                .RandomlyWalks()
                .WithFrequency(10)

                .AddSpawn(ObjectType.Creature, "vdathshaman")
                .RandomlyWalks()
                .WithFrequency(50)

                .AddSpawn(ObjectType.Creature, "vdathsquell")
                .RandomlyWalks()
                .WithFrequency(70);
        }

        private void GrottosBoss()
        {
            _builder.Create("DATHOMIR_GROTTOS_BOSS")

                .AddSpawn(ObjectType.Creature, "vdathdarkadept")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(60 + Random.D100(1))

                .AddSpawn(ObjectType.Creature, "vdatthrancor")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(60 + Random.D100(1))

                .AddSpawn(ObjectType.Creature, "vdathchirodac")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(60 + Random.D100(1));
        }

        private void MountainCaves()
        {
            _builder.Create("DATHOMIR_MOUNTAIN_CAVES")
                .AddSpawn(ObjectType.Creature, "vdathsprantal")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathsquell")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathssurian")
                .RandomlyWalks()
                .WithFrequency(10);
        }

        private void Mountains()
        {
            _builder.Create("DATHOMIR_MOUNTAINS")
                .AddSpawn(ObjectType.Creature, "vdathsprantal")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathsquell")
                .RandomlyWalks()
                .WithFrequency(100);
        }

        private void RuinBase()
        {
            _builder.Create("DATHOMIR_RUIN_BASE")
                .AddSpawn(ObjectType.Creature, "vdathpurbole")
                .RandomlyWalks()
                .WithFrequency(80)

                .AddSpawn(ObjectType.Creature, "vdathshaman")
                .RandomlyWalks()
                .WithFrequency(50)

                .AddSpawn(ObjectType.Creature, "vdathguard")
                .RandomlyWalks()
                .WithFrequency(100);
        }

        private void TarnishedJungles()
        {
            _builder.Create("DATHOMIR_TARNISHED_JUNGLES")
                .AddSpawn(ObjectType.Creature, "vdathswampland")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathshear")
                .RandomlyWalks()
                .WithFrequency(100);
        }

        private void TarnishedJunglesNorth()
        {
            _builder.Create("DATHOMIR_TARNISHED_JUNGLES_NORTH")
                .AddSpawn(ObjectType.Creature, "vdathswampland")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathshear")
                .RandomlyWalks()
                .WithFrequency(100);
        }

        private void TribeVillage()
        {
            _builder.Create("DATHOMIR_TRIBE_VILLAGE")
                .AddSpawn(ObjectType.Creature, "vdathguard")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathshaman")
                .RandomlyWalks()
                .WithFrequency(20)

                .AddSpawn(ObjectType.Creature, "vdathtribal")
                .RandomlyWalks()
                .WithFrequency(100);
        }
        private void HiddenTunnel()
        {
            _builder.Create("DATHOMIR_HIDDEN_TUNNEL")
                .AddSpawn(ObjectType.Creature, "vgapingspider")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "vdathshaman")
                .RandomlyWalks()
                .WithFrequency(20)

                .AddSpawn(ObjectType.Creature, "vdathtribal")
                .RandomlyWalks()
                .WithFrequency(100);

        }

        private void DathomirTarnJunglePreserve()
        {
            // Adepts hold the spawn points nearest the entrance and specialists the deeper ones, so each tier of
            // the line is found in its own part of the dungeon.
            _builder.Create("CAPSTONE_DATHOMIR_TARN_JUNGLE_PRESERVE", "Dathomir Tarn Jungle Preserve - Capstone Adepts")
                .AddSpawn(ObjectType.Creature, "cp_apexbite_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_unbrbeast_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_alpharhy_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome();

            _builder.Create("CAPSTONE_DATHOMIR_TARN_JUNGLE_PRESERVE_DEEP", "Dathomir Tarn Jungle Preserve - Capstone Specialists")
                .AddSpawn(ObjectType.Creature, "cp_apexbite_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_unbrbeast_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_alpharhy_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();

            // Each inner circle has its own table on four fixed spawn waypoints, so a circle step always
            // has its four kills present instead of depending on rolls from the general table.
            _builder.Create("CAPSTONE_APEXBITE_IC_SPAWN", "Dathomir Tarn Jungle Preserve - Apex Bite Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_apexbite_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_UNBRBEAST_IC_SPAWN", "Dathomir Tarn Jungle Preserve - Unbreakable Beast Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_unbrbeast_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_ALPHARHY_IC_SPAWN", "Dathomir Tarn Jungle Preserve - Alpha Rhythm Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_alpharhy_ic")
                .WithFrequency(1)
                .ReturnsHome();
        }

        private void DathomirGrottoApexDen()
        {
            // Adepts hold the spawn points nearest the entrance and specialists the deeper ones, so each tier of
            // the line is found in its own part of the dungeon.
            _builder.Create("CAPSTONE_DATHOMIR_GROTTO_APEX_DEN", "Dathomir Grotto Apex Den - Capstone Adepts")
                .AddSpawn(ObjectType.Creature, "cp_primover_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_untinst_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_forcebeast_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome();

            _builder.Create("CAPSTONE_DATHOMIR_GROTTO_APEX_DEN_DEEP", "Dathomir Grotto Apex Den - Capstone Specialists")
                .AddSpawn(ObjectType.Creature, "cp_primover_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_untinst_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_forcebeast_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();

            // Each inner circle has its own table on four fixed spawn waypoints, so a circle step always
            // has its four kills present instead of depending on rolls from the general table.
            _builder.Create("CAPSTONE_PRIMOVER_IC_SPAWN", "Dathomir Grotto Apex Den - Primal Overrun Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_primover_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_UNTINST_IC_SPAWN", "Dathomir Grotto Apex Den - Untouchable Instinct Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_untinst_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_FORCEBEAST_IC_SPAWN", "Dathomir Grotto Apex Den - Force-Bonded Beast Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_forcebeast_ic")
                .WithFrequency(1)
                .ReturnsHome();
        }
    }
}
