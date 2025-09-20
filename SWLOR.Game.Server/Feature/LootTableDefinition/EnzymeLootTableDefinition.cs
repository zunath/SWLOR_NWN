using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Core.Bioware;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class EnzymeLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        private static readonly int[] _primaryWeights =
        {
            70, // 0.1% to 1.0%  (1-10)
            50, // 1.1% to 2.0%  (11-20)
            20, // 2.1% to 3.0%  (21-30)
            15, // 3.1% to 4.0%  (31-40)
            10, // 4.1% to 5.0%  (41-50)
            8,  // 5.1% to 6.0%  (51-60)
            6,  // 6.1% to 7.0%  (61-70)
            4,  // 7.1% to 8.0%  (71-80)
            2,  // 8.1% to 9.0%  (81-90)
            1,  // 9.1% to 10.0% (91-100)
        };

        private static readonly int[] _secondaryWeights =
        {
            50, // 0.1% to 1.0%  (1-10)
            20, // 1.1% to 2.0%  (11-20)
            15, // 2.1% to 3.0%  (21-30)
            10, // 3.1% to 4.0%  (31-40)
            8,  // 4.1% to 5.0%  (41-50)
        };

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Hydrolase();
            Isomerase();
            Lyase();

            return _builder.Build();
        }

        private int GetRandomValueBetweenRange(int index)
        {
            switch (index)
            {
                case 0: // 0.1% to 1.0%  (1-10)
                    return Random.Next(1, 10);
                case 1: // 1.1% to 2.0%  (11-20)
                    return Random.Next(11, 20);
                case 2: // 2.1% to 3.0%  (21-30)
                    return Random.Next(21, 30);
                case 3: // 3.1% to 4.0%  (31-40)
                    return Random.Next(31, 40);
                case 4: // 4.1% to 5.0%  (41-50)
                    return Random.Next(41, 50);
                case 5: // 5.1% to 6.0%  (51-60)
                    return Random.Next(51, 60);
                case 6: // 6.1% to 7.0%  (61-70)
                    return Random.Next(61, 70);
                case 7: // 7.1% to 8.0%  (71-80)
                    return Random.Next(71, 80);
                case 8: // 8.1% to 9.0%  (81-90)
                    return Random.Next(81, 90);
                case 9: // 9.1% to 10.0% (91-100)
                    return Random.Next(91, 100);
                default:
                    return 0;
            }
        }

        private List<IncubationStatType> GetDefaultHydrolasePrimaryStats()
        {
            return new List<IncubationStatType>
            {
                IncubationStatType.LearningPurity,
                IncubationStatType.AttackPurity,
                IncubationStatType.FortitudePurity,
                IncubationStatType.ElectricalDefensePurity,
                IncubationStatType.PhysicalDefensePurity
            };
        }
        private List<IncubationStatType> GetDefaultHydrolaseSecondaryStats()
        {
            return new List<IncubationStatType>
            {
                IncubationStatType.MutationChance,
                IncubationStatType.XPPenalty,
                IncubationStatType.EvasionPurity,
                IncubationStatType.PoisonDefensePurity,
                IncubationStatType.FireDefensePurity,
                IncubationStatType.WillPurity,
                IncubationStatType.AccuracyPurity,
                IncubationStatType.ForceDefensePurity,
                IncubationStatType.IceDefensePurity,
                IncubationStatType.ReflexPurity
            };
        }

        private List<IncubationStatType> GetDefaultIsomerasePrimaryStats()
        {
            return new List<IncubationStatType>
            {
                IncubationStatType.XPPenalty,
                IncubationStatType.EvasionPurity,
                IncubationStatType.PoisonDefensePurity,
                IncubationStatType.FireDefensePurity,
                IncubationStatType.WillPurity
            };
        }
        private List<IncubationStatType> GetDefaultIsomeraseSecondaryStats()
        {
            return new List<IncubationStatType>
            {
                IncubationStatType.MutationChance,
                IncubationStatType.LearningPurity,
                IncubationStatType.AttackPurity,
                IncubationStatType.FortitudePurity,
                IncubationStatType.ElectricalDefensePurity,
                IncubationStatType.PhysicalDefensePurity,
                IncubationStatType.AccuracyPurity,
                IncubationStatType.ForceDefensePurity,
                IncubationStatType.IceDefensePurity,
                IncubationStatType.ReflexPurity
            };
        }

        private List<IncubationStatType> GetDefaultLyasePrimaryStats()
        {
            return new List<IncubationStatType>
            {
                IncubationStatType.MutationChance,
                IncubationStatType.AccuracyPurity,
                IncubationStatType.ForceDefensePurity,
                IncubationStatType.IceDefensePurity,
                IncubationStatType.ReflexPurity
            };
        }
        private List<IncubationStatType> GetDefaultLyaseSecondaryStats()
        {
            return new List<IncubationStatType>
            {
                IncubationStatType.XPPenalty,
                IncubationStatType.LearningPurity,
                IncubationStatType.AttackPurity,
                IncubationStatType.FortitudePurity,
                IncubationStatType.ElectricalDefensePurity,
                IncubationStatType.PhysicalDefensePurity,
                IncubationStatType.EvasionPurity,
                IncubationStatType.PoisonDefensePurity,
                IncubationStatType.FireDefensePurity,
                IncubationStatType.WillPurity
            };
        }

        private void SpawnAction(
            uint item, 
            IList<IncubationStatType> primaryStats, 
            IList<IncubationStatType> secondaryStats)
        {
            var primaryIndex = Random.GetRandomWeightedIndex(_primaryWeights);
            var secondaryIndex = Random.GetRandomWeightedIndex(_secondaryWeights);
            var primaryCount = Random.Next(0, 3);
            var secondaryCount = 1 + Random.Next(0, 3);

            for (var propCount = 1; propCount <= primaryCount; propCount++)
            {
                if (primaryStats.Count <= 0)
                    break;

                var primaryAmount = GetRandomValueBetweenRange(primaryIndex);
                var index = Random.Next(primaryStats.Count);
                var subType = primaryStats[index];
                var ip = ItemPropertyCustom(ItemPropertyType.Incubation, (int)subType, primaryAmount);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

                primaryStats.RemoveAt(index);
            }

            for (var propCount = 1; propCount <= secondaryCount; propCount++)
            {
                if (secondaryStats.Count <= 0)
                    break;

                var secondaryAmount = GetRandomValueBetweenRange(secondaryIndex);
                var index = Random.Next(secondaryStats.Count);
                var subType = secondaryStats[index];
                var ip = ItemPropertyCustom(ItemPropertyType.Incubation, (int)subType, secondaryAmount);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

                secondaryStats.RemoveAt(index);
            }
        }

        private void Hydrolase()
        {
            void HydrolaseSpawn(uint item)
            {
                SpawnAction(item, GetDefaultHydrolasePrimaryStats(), GetDefaultHydrolaseSecondaryStats());
            }

            _builder.Create("HYDROLASE_TYPE_1")
                .AddItem("hydrolase_blue", 100)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_orange", 90)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_red", 70)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_purple", 20)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_white", 5)
                .AddSpawnAction(HydrolaseSpawn);
        }

        private void Isomerase()
        {
            void IsomeraseSpawn(uint item)
            {
                SpawnAction(item, GetDefaultIsomerasePrimaryStats(), GetDefaultIsomeraseSecondaryStats());
            }

            _builder.Create("ISOMERASE_TYPE_1")
                .AddItem("isomerase_orange", 100)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_blue", 90)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_green", 70)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_red", 20)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_yellow", 5)
                .AddSpawnAction(IsomeraseSpawn);

            _builder.Create("ISOMERASE_TYPE_2")
                .AddItem("isomerase_orange", 5)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_blue", 20)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_green", 70)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_red", 90)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_yellow", 100)
                .AddSpawnAction(IsomeraseSpawn);
        }

        private void Lyase()
        {
            void LyaseSpawn(uint item)
            {
                SpawnAction(item, GetDefaultLyasePrimaryStats(), GetDefaultLyaseSecondaryStats());
            }

            _builder.Create("LYASE_TYPE_1")
                .AddItem("lyase_green", 100)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_blue", 90)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_orange", 70)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_red", 20)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_black", 5)
                .AddSpawnAction(LyaseSpawn);
        }
    }
}
