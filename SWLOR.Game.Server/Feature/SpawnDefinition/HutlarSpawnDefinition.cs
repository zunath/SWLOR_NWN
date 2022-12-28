using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

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

            return _builder.Build();
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
            _builder.Create("HutlarDungeonBroodmother")
                .AddSpawn(ObjectType.Creature, "huthivebroodmoth")
                .WithFrequency(1)
                .RespawnDelay(120);

            _builder.Create("HutlarDungeonChieftain")
                .AddSpawn(ObjectType.Creature, "byysk_chieftain")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("HutlarDungeonShaman")
                .AddSpawn(ObjectType.Creature, "byysk_shaman")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("HutlarDungeonChampion")
                .AddSpawn(ObjectType.Creature, "byysk_champion")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("HutlarDungeonByyskGuardian")
                .AddSpawn(ObjectType.Creature, "byysk_guard001")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(2)

                .AddSpawn(ObjectType.Creature, "byysk_guard002")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(2);

            _builder.Create("HutlarDungeonHiveSlug")
                .AddSpawn(ObjectType.Creature, "qion_slug001")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(4);

            _builder.Create("HutlarDungeonHiveTunneler")
                .AddSpawn(ObjectType.Creature, "qion_hive_tunnel")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(4);
        }
    }
}
