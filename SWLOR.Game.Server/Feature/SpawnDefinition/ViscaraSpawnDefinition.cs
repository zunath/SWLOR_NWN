using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class ViscaraSpawnDefinition: ISpawnListDefinition
    {
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder();
            Wildlands(builder);
            MandalorianRaiders(builder);
            MandalorianLeader(builder);
            WildwoodsLooters(builder);
            WildwoodsKinrath(builder);
            WildwoodsGimpassa(builder);
            ValleyCairnmogs(builder);
            CoxxionFlesheaters(builder);
            CoxxionFleshleader(builder);
            DeepMountainRaivors(builder);
            CrystalSpiders(builder);

            return builder.Build();
        }

        private void Wildlands(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_WILDLANDS", "Wildlands")
                .AddSpawn(ObjectType.Creature, "warocas")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Creature, "kath_hound")
                .WithFrequency(70);
        }

        private void MandalorianRaiders(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_MANDALORIAN_RAIDERS", "Mandalorian Raiders")
                .AddSpawn(ObjectType.Creature, "man_warrior_1")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Creature, "man_warrior_2")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Creature, "man_ranger_1")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Creature, "man_ranger_2")
                .WithFrequency(30);
        }

        private void MandalorianLeader(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_MANDALORIAN_LEADER", "Mandalorian Leader")
                .AddSpawn(ObjectType.Creature, "man_leader")
                .WithFrequency(100);
        }

        private void WildwoodsLooters(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_WILDWOODS_LOOTERS", "Wildwoods Looters")
                .AddSpawn(ObjectType.Creature, "looter_1")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Creature, "looter_2")
                .WithFrequency(30);
        }

        private void WildwoodsKinrath(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_WILDWOODS_KINRATH", "Wildwoods Kinrath")
                .AddSpawn(ObjectType.Creature, "ww_kinrath")
                .WithFrequency(30);
        }

        private void WildwoodsGimpassa(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_WILDWOODS_GIMPASSA", "Wildwoods Gimpassa")
                .AddSpawn(ObjectType.Creature, "ww_gimpassa")
                .WithFrequency(30);
        }

        private void ValleyCairnmogs(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_VALLEY_CAIRNMOGS", "Valley Cairnmogs")
                .AddSpawn(ObjectType.Creature, "vall_nashtah")
                .WithFrequency(20)
                
                .AddSpawn(ObjectType.Creature, "valley_cairnmog")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Creature, "valley_cairnmog2")
                .WithFrequency(50);
        }

        private void CoxxionFlesheaters(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_COXXION_FLESHEATERS", "Coxxion Flesheaters")
                .AddSpawn(ObjectType.Creature, "v_flesheater")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Creature, "v_flesheater2")
                .WithFrequency(10);
        }

        private void CoxxionFleshleader(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_COXXION_FLESHLEADER", "Coxxion Fleshleader")
                .AddSpawn(ObjectType.Creature, "v_fleshleader")
                .WithFrequency(100);
        }

        private void DeepMountainRaivors(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_DEEPMOUNTAIN_RAIVORS", "Deep Mountain Raivors")
                .AddSpawn(ObjectType.Creature, "v_raivor")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Creature, "v_raivor2")
                .WithFrequency(10);
        }

        private void CrystalSpiders(SpawnTableBuilder builder)
        {
            builder.Create("VISCARA_CRYSTAL_SPIDERS", "Crystal Spiders")
                .AddSpawn(ObjectType.Creature, "crystalspider")
                .WithFrequency(10);
        }
        
    }
}
