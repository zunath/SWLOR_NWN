using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class HutlarSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            Byysk();
            QionAnimals();
            Valley();
            Wastes();
            FrozenCave();
            QionFoothills();
            QionHive();
            HutlarQionTestSite();

            HutqionRareElites();
            return _builder.Build();
        }

        private void HutqionRareElites()
        {
            _builder.Create("HUTLAR_QION_TEST_SITE_RARES", "Hutlar Qion Test Site - Rare Elites")
                .AddSpawn(ObjectType.Creature, "flurrychamp").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "thermlancer").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome()
                .AddSpawn(ObjectType.Creature, "barrieroverse").WithFrequency(1).AsRare().RandomlyWalks().ReturnsHome();
        }

        private void Byysk()
        {
            _builder.Create("HUTLAR_BYYSK", "Byysk")
                .AddSpawn(ObjectType.Creature, "byysk_warrior")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "byysk_warrior2")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void QionAnimals()
        {
            _builder.Create("HUTLAR_QION_ANIMALS", "Qion Animals")
                .AddSpawn(ObjectType.Creature, "qion_slug")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "qion_tiger")
                .WithFrequency(8)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Valley()
        {
            _builder.Create("HUTLAR_QION_VALLEY", "Hutlar Valley")
                .AddSpawn(ObjectType.Creature, "qion_slug")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "qion_tiger")
                .WithFrequency(8)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Wastes()
        {
            _builder.Create("HUTLAR_WASTES", "Hutlar Wastes")
                .AddSpawn(ObjectType.Creature, "qion_tiger")
                .WithFrequency(8)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void FrozenCave()
        {
            _builder.Create("HUTLAR_FROZEN_CAVE", "Hutlar Frozen Cave")
                .AddSpawn(ObjectType.Creature, "byysk_warrior")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "byysk_warrior2")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "byysk_cryoadept")
                .WithFrequency(4)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "qion_tiger")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void QionFoothills()
        {
            _builder.Create("HUTLAR_QION_FOOTHILLS", "Hutlar Qion Foothills")
                .AddSpawn(ObjectType.Creature, "byysk_warrior")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "byysk_warrior2")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "byysk_cryoadept")
                .WithFrequency(4)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "qion_slug")
                .WithFrequency(15)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "qion_tiger")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void QionHive()
        {
            _builder.Create("HUTLAR_DUNGEON_BROODMOTHER")
                .AddSpawn(ObjectType.Creature, "huthivebroodmoth")
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(120);

            _builder.Create("HUTLAR_DUNGEON_CHIEFTAIN")
                .AddSpawn(ObjectType.Creature, "byysk_chieftain")
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(20);

            _builder.Create("HUTLAR_DUNGEON_SHAMAN")
                .AddSpawn(ObjectType.Creature, "byysk_shaman")
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(20);

            _builder.Create("HUTLAR_DUNGEON_CHAMPION")
                .AddSpawn(ObjectType.Creature, "byysk_champion")
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(20);

            _builder.Create("HUTLAR_DUNGEON_BYYSKGUARDIAN")
                .AddSpawn(ObjectType.Creature, "byysk_guard001")
                .RandomlyWalks()
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(2)

                .AddSpawn(ObjectType.Creature, "byysk_guard002")
                .RandomlyWalks()
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(2);

            _builder.Create("HUTLAR_DUNGEON_SLUG")
                .AddSpawn(ObjectType.Creature, "qion_slug001")
                .RandomlyWalks()
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(4);

            _builder.Create("HUTLAR_DUNGEON_TUNNELER")
                .AddSpawn(ObjectType.Creature, "qion_hive_tunnel")
                .RandomlyWalks()
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(4);
        }

        private void HutlarQionTestSite()
        {
            _builder.Create("CAPSTONE_HUTLAR_QION_TEST_SITE", "Hutlar Qion Test Site - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_thermdet_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_thermdet_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_thermdet_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_overbarr_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_overbarr_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_overbarr_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_perflurry_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_perflurry_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_perflurry_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
