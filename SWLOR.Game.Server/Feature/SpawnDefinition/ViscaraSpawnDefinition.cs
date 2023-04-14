using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class ViscaraSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new SpawnTableBuilder();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            Wildlands();
            MandalorianRaiders();
            MandalorianLeader();
            MandalorianHunter();
            MandalorianScout();
            WildwoodsLooters();
            WildwoodsKinrath();
            WildwoodsGimpassa();
            ValleyCairnmogs();
            CoxxionFlesheaters();
            CoxxionFleshleader();
            DeepMountainRaivors();
            CrystalSpiders();
            Swamplands();
            VelesSewers();
            TwilightPraxeum();

            return _builder.Build();
        }

        private void Wildlands()
        {
            _builder.Create("VISCARA_WILDLANDS", "Wildlands")
                .AddSpawn(ObjectType.Creature, "warocas")
                .WithFrequency(40)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "kath_hound")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void MandalorianRaiders()
        {
            _builder.Create("VISCARA_MANDALORIAN_RAIDERS", "Mandalorian Raiders")
                .AddSpawn(ObjectType.Creature, "man_warrior_1")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "man_warrior_2")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "man_ranger_1")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "man_ranger_2")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void MandalorianLeader()
        {
            _builder.Create("VISCARA_MANDALORIAN_LEADER", "Mandalorian Leader")
                .AddSpawn(ObjectType.Creature, "man_leader")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void MandalorianHunter()
        {
            _builder.Create("VISCARA_WILDWOODS_MANDO_HUNTER", "Mandalorian Hunter")
                .AddSpawn(ObjectType.Creature, "man_hunter")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void MandalorianScout()
        {
            _builder.Create("VISCARA_WILDWOODS_MANDO_SCOUT", "Mandalorian Scout")
                .AddSpawn(ObjectType.Creature, "man_scout")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void WildwoodsLooters()
        {
            _builder.Create("VISCARA_WILDWOODS_LOOTERS", "Wildwoods Looters")
                .AddSpawn(ObjectType.Creature, "looter_1")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "looter_2")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void WildwoodsKinrath()
        {
            _builder.Create("VISCARA_WILDWOODS_KINRATH", "Wildwoods Kinrath")
                .AddSpawn(ObjectType.Creature, "ww_kinrath")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void WildwoodsGimpassa()
        {
            _builder.Create("VISCARA_WILDWOODS_GIMPASSA", "Wildwoods Gimpassa")
                .AddSpawn(ObjectType.Creature, "ww_gimpassa")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void ValleyCairnmogs()
        {
            _builder.Create("VISCARA_VALLEY_CAIRNMOGS", "Valley Cairnmogs")
                .AddSpawn(ObjectType.Creature, "vall_nashtah")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "valley_cairnmog")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "valley_cairnmog2")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void CoxxionFlesheaters()
        {
            _builder.Create("VISCARA_COXXION_FLESHEATERS", "Coxxion Flesheaters")
                .AddSpawn(ObjectType.Creature, "v_flesheater")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "v_flesheater2")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void CoxxionFleshleader()
        {
            _builder.Create("VISCARA_COXXION_FLESHLEADER", "Coxxion Fleshleader")
                .AddSpawn(ObjectType.Creature, "v_fleshleader")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void DeepMountainRaivors()
        {
            _builder.Create("VISCARA_DEEPMOUNTAIN_RAIVORS", "Deep Mountain Raivors")
                .AddSpawn(ObjectType.Creature, "v_raivor")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "v_raivor2")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void CrystalSpiders()
        {
            _builder.Create("VISCARA_CRYSTAL_SPIDERS", "Crystal Spiders")
                .AddSpawn(ObjectType.Creature, "crystalspider")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Swamplands()
        {
            _builder.Create("VISCARA_SWAMPLANDS", "Swamplands")
                .AddSpawn(ObjectType.Creature, "swampvines")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "v_flesheater")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "v_flesheater2")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void VelesSewers()
        {
            _builder.Create("VISCARA_VELES_SEWERS", "Veles Sewers")
                .AddSpawn(ObjectType.Creature, "looter_1")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "looter_2")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void TwilightPraxeum()
        {
            _builder.Create("VISCARA_REVANITE_MAZE")
                .AddSpawn(ObjectType.Creature, "revmynock")
                .WithFrequency(1)
                .RespawnDelay(20)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
