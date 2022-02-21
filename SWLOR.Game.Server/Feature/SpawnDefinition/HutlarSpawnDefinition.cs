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
            Frzcave();

            return _builder.Build();
        }

        private void Byysk()
        {
            _builder.Create("HUTLAR_BYYSK", "Byysk")
                .AddSpawn(ObjectType.Creature, "byysk_warrior")
                .WithFrequency(10)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "byysk_warrior2")
                .WithFrequency(10)
                .RandomlyWalks();
        }

        private void QionAnimals()
        {
            _builder.Create("HUTLAR_QION_ANIMALS", "Qion Animals")
                .AddSpawn(ObjectType.Creature, "qion_slug")
                .WithFrequency(10)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "qion_tiger")
                .WithFrequency(8)
                .RandomlyWalks();
        }

        private void Valley()
        {
            _builder.Create("HUTLAR_QION_VALLEY", "Hutlar Valley")
                .AddSpawn(ObjectType.Creature, "qion_slug")
                .WithFrequency(10)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "qion_tiger")
                .WithFrequency(8)
                .RandomlyWalks();
        }
        private void Wastes()
        {
            _builder.Create("HUTLAR_WASTES", "Hutlar Frozen Wastes")
                .AddSpawn(ObjectType.Creature, "frost_saber")
                .WithFrequency(8)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "frost_saber_cub")
                .WithFrequency(6)
                .RandomlyWalks();
        }
        private void Frzcave()
        {
            _builder.Create("HUTLAR_FRZ_CAVE", "Hutlar Frozen Cave")
                .AddSpawn(ObjectType.Creature, "cave_yeti")
                .WithFrequency(8)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "cave_yeti_sm")
                .WithFrequency(6)
                .RandomlyWalks();
        }
    }
}
