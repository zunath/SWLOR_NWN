using System.Collections.Generic;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

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
            WildwoodsNorthMandalorianScout();
            WildwoodsRuinedMandalorianHunter();
            ValleyCairnmogs();
            CoxxionFlesheaters();
            CoxxionFleshleader();
            DeepMountainRaivors();
            CrystalSpiders();
            Swamplands();
            WesternSwamplands();
            EasternSwamplands();
            SewersDepthsGeneral();
            Veles();
            VelesSewers();
            TwilightPraxeum();
            VelesMilitiaAnnex();
            ViscaraRepublicEngineeringBunker();

            VelesRareElites();
            VisbunkerRareElites();
            return _builder.Build();
        }

        private void VisbunkerRareElites()
        {
            _builder.Create("VISCARA_REPUBLIC_ENGINEERING_BUNKER_RARES", "Viscara Republic Engineering Bunker - Rare Elites")
                .AddSpawn(ObjectType.Creature, "bunkerbreak").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "beaconmarks").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "decurioncmd").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void VelesRareElites()
        {
            _builder.Create("VELES_MILITIA_ANNEX_RARES", "Veles Militia Annex - Rare Elites")
                .AddSpawn(ObjectType.Creature, "invictus").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "ruptorvane").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "blackoutwrd").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "oldscar_kath")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "stormplume")
                .WithFrequency(1)
                .AsRare()
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "harrek_voss")
                .WithFrequency(1)
                .AsRare()
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "nara_venn")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void WildwoodsKinrath()
        {
            _builder.Create("VISCARA_WILDWOODS_KINRATH", "Wildwoods Kinrath")
                .AddSpawn(ObjectType.Creature, "ww_kinrath")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "silkshade")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void WildwoodsGimpassa()
        {
            _builder.Create("VISCARA_WILDWOODS_GIMPASSA", "Wildwoods Gimpassa")
                .AddSpawn(ObjectType.Creature, "ww_gimpassa")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mossback")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void WildwoodsNorthMandalorianScout()
        {
            _builder.Create("VISCARA_WILDWOODS_NORTH_SCOUT", "Wildwoods North Mandalorian Scout")
                .AddSpawn(ObjectType.Creature, "man_scout")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "tarn_kyric")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void WildwoodsRuinedMandalorianHunter()
        {
            _builder.Create("VISCARA_WILDWOODS_RUINED_HUNTER", "Wildwoods Ruined Mandalorian Hunter")
                .AddSpawn(ObjectType.Creature, "man_hunter")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "varo_skeld")
                .WithFrequency(1)
                .AsRare()
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "greyspine")
                .WithFrequency(1)
                .AsRare()
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "maw_ghal")
                .WithFrequency(1)
                .AsRare()
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "redtail_kor")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void CrystalSpiders()
        {
            _builder.Create("VISCARA_CRYSTAL_SPIDERS", "Crystal Spiders")
                .AddSpawn(ObjectType.Creature, "crystalspider")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "shardeye")
                .WithFrequency(1)
                .AsRare()
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

        private void WesternSwamplands()
        {
            _builder.Create("VISCARA_WESTERN_SWAMPLANDS", "Western Swamplands")
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "rootcoil")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void EasternSwamplands()
        {
            _builder.Create("VISCARA_EASTERN_SWAMPLANDS", "Eastern Swamplands")
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mirevein")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void SewersDepthsGeneral()
        {
            _builder.Create("VISCARA_SEWERS_DEPTHS_GENERAL", "Viscara Sewers Depths - General")
                .AddSpawn(ObjectType.Creature, "bf_scavenger")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "bf_pulsedroid")
                .WithFrequency(40)
                .RandomlyWalks()
                .ReturnsHome()
                .PlayAnimation(DurationType.Instant, AnimationEvent.CreatureOnDeath, VisualEffect.Fnf_Fireball)

                .AddSpawn(ObjectType.Creature, "bf_duelist")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "vrix7")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Veles()
        {
            _builder.Create("VELES_COLONIST")
                .AddSpawn(ObjectType.Creature, "colonistbith")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistbothan")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistbothan2")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistduro")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistgran")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistkeldor")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistmoncala")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistrodian")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistrodian2")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonisttrando")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonisttrando2")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonisttwilek")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonisttwilek3")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonistwq")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonisthuman1")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "colonisthuman2")
                .WithFrequency(20)
                .RandomlyWalks();
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "soot_rusk")
                .WithFrequency(1)
                .AsRare()
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "ashwing")
                .WithFrequency(1)
                .AsRare()
                .RespawnDelay(20)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void VelesMilitiaAnnex()
        {
            _builder.Create("CAPSTONE_VELES_MILITIA_ANNEX", "Veles Militia Annex - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_invinc_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_invinc_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_invinc_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_vitrupt_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_vitrupt_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_vitrupt_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_sysshut_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_sysshut_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_sysshut_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void ViscaraRepublicEngineeringBunker()
        {
            _builder.Create("CAPSTONE_VISCARA_REPUBLIC_ENGINEERING_BUNKER", "Viscara Republic Engineering Bunker - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_killbeacon_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_killbeacon_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_killbeacon_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_embunker_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_embunker_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_embunker_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_deccommand_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_deccommand_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_deccommand_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
