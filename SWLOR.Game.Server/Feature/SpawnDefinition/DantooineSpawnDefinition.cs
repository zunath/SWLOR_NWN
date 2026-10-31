using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class DantooineSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            DantooineKinrathCaves();
            DantooineJungle();
            DantooineRuinFarm();
            DantooineLake();
            DantooineJanta();
            DantooineKinrathBoss();
            DantooineMountains();
            DantooineCanyon();
            DantooineWorldBoss();
            DantooineSouthPlains();
            HutlarJediTrialHalls();
            MonCalaMedicalSublevel();

            DanenclaveRareElites();
            DanmedRareElites();
            return _builder.Build();
        }

        private void DanmedRareElites()
        {
            _builder.Create("DANTOOINE_MEDICAL_SUBLEVEL_RARES", "Dantooine Medical Sublevel - Rare Elites")
                .AddSpawn(ObjectType.Creature, "triagewarden").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "chemslinger").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "conduitmatrn").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void DanenclaveRareElites()
        {
            _builder.Create("DANTOOINE_JEDI_ENCLAVE_TRIAL_HALLS_RARES", "Dantooine Jedi Enclave Trial Halls - Rare Elites")
                .AddSpawn(ObjectType.Creature, "sabraetrial").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "enclavesentl").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "cycloneadpt").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void DantooineKinrathCaves()
        {
            _builder.Create("DANTOOINE_KINRATH_CAVES")
                .AddSpawn(ObjectType.Creature, "hkinrath")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineJungle()
        {
            _builder.Create("DANTOOINE_JUNGLE")
                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "hkinrath")
                .RandomlyWalks()
                .WithFrequency(20)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(5)
                .ReturnsHome();
        }

        private void DantooineRuinFarm()
        {
            _builder.Create("DANTOOINE_RUIN_FARM")
                .AddSpawn(ObjectType.Creature, "pthune")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(20)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(5)
                .ReturnsHome();
        }

        private void DantooineLake()
        {
            _builder.Create("DANTOOINE_LAKE")
                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(5)

                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "hkinrath")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineJanta()
        {
            _builder.Create("DANTOOINE_JANTA")
                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "voritorlizard")
                .RandomlyWalks()
                .WithFrequency(50)
                .ReturnsHome();
        }

        private void DantooineKinrathBoss()
        {
            _builder.Create("DANTOOINE_KIN_BOSS")
                .AddSpawn(ObjectType.Creature, "vqueenkin")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(20 + Random.D100(1))
                .ReturnsHome();
        }

        private void DantooineMountains()
        {
            _builder.Create("DANTOOINE_MOUNTAINS")
                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineCanyon()
        {
            _builder.Create("DANTOOINE_CANYON")
                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineSouthPlains()
        {
            _builder.Create("DANTOOINE_SOUTH_PLAINS")
                .AddSpawn(ObjectType.Creature, "dantarihunter")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "dantarishaman")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineWorldBoss()
        {
            _builder.Create("DANTOOINE_WORLD_BOSS")
                .AddSpawn(ObjectType.Creature, "bolboss")
                .RandomlyWalks()
                .WithFrequency(16)
                .RespawnDelay(60 + Random.D100(1))
                .AddSpawn(ObjectType.Creature, "dgraul")
                .RandomlyWalks()
                .WithFrequency(3)
                .RespawnDelay(60 + Random.D100(1));

        }

        private void HutlarJediTrialHalls()
        {
            _builder.Create("CAPSTONE_DANTOOINE_JEDI_ENCLAVE_TRIAL_HALLS", "Dantooine Jedi Enclave Trial Halls - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_sabstorm_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_sabstorm_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_guardmst_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_guardmst_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_sabcycl_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_sabcycl_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();

            // Each inner circle has its own table on four fixed spawn waypoints, so a circle step always
            // has its four kills present instead of depending on rolls from the general table.
            _builder.Create("CAPSTONE_SABSTORM_IC_SPAWN", "Dantooine Jedi Enclave Trial Halls - Saber Storm Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_sabstorm_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_GUARDMST_IC_SPAWN", "Dantooine Jedi Enclave Trial Halls - Guardian Master Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_guardmst_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_SABCYCL_IC_SPAWN", "Dantooine Jedi Enclave Trial Halls - Saber Cyclone Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_sabcycl_ic")
                .WithFrequency(1)
                .ReturnsHome();
        }

        private void MonCalaMedicalSublevel()
        {
            _builder.Create("CAPSTONE_DANTOOINE_MEDICAL_SUBLEVEL", "Dantooine Medical Sublevel - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_emcocktail_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_emcocktail_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_holdline_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_holdline_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_infconduit_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_infconduit_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();

            // Each inner circle has its own table on four fixed spawn waypoints, so a circle step always
            // has its four kills present instead of depending on rolls from the general table.
            _builder.Create("CAPSTONE_EMCOCKTAIL_IC_SPAWN", "Dantooine Medical Sublevel - Emergency Cocktail Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_emcocktail_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_HOLDLINE_IC_SPAWN", "Dantooine Medical Sublevel - Hold the Line Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_holdline_ic")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("CAPSTONE_INFCONDUIT_IC_SPAWN", "Dantooine Medical Sublevel - Infinite Conduit Inner Circle")
                .AddSpawn(ObjectType.Creature, "cp_infconduit_ic")
                .WithFrequency(1)
                .ReturnsHome();
        }
    }
}
