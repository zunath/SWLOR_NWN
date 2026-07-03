using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public static class BeastResistanceCalculator
    {
        private const string DefensePuritySuffix = "DefensePurity";
        private const string ResistancePuritySuffix = "ResistancePurity";

        public static Dictionary<CombatDamageType, int> CreateRandomDefensePurities(
            int minimumInclusive = 0,
            int maximumExclusive = 10)
        {
            return CombatDamageTypes.GetDefenseDamageTypes()
                .ToDictionary(type => type, _ => Random.Next(minimumInclusive, maximumExclusive));
        }

        public static Dictionary<CombatDamageType, int> CreateDefensePurities(
            IReadOnlyDictionary<CombatDamageType, int> defensePurities)
        {
            var purities = CombatDamageTypes.CreateDefaultDefenseValues();

            foreach (var type in purities.Keys.ToList())
            {
                purities[type] = GetValue(defensePurities, type);
            }

            return purities;
        }

        public static Dictionary<ResistanceType, int> CreateRandomResistancePurities(
            int minimumInclusive = 0,
            int maximumExclusive = 10)
        {
            return Resistance.GetAllResistanceTypes()
                .ToDictionary(type => type, _ => Random.Next(minimumInclusive, maximumExclusive));
        }

        public static Dictionary<ResistanceType, int> CreateResistancePurities(
            IReadOnlyDictionary<ResistanceType, int> resistancePurities)
        {
            var purities = Resistance.CreateDefaultResistanceValues();

            foreach (var type in purities.Keys.ToList())
            {
                purities[type] = GetValue(resistancePurities, type);
            }

            return purities;
        }

        public static bool TryGetDefenseType(
            IncubationStatType incubationStatType,
            out CombatDamageType damageType)
        {
            damageType = CombatDamageType.Invalid;

            var name = incubationStatType.ToString();
            if (!name.EndsWith(DefensePuritySuffix))
                return false;

            var damageTypeName = name[..^DefensePuritySuffix.Length];
            if (!Enum.TryParse(damageTypeName, out damageType))
                return false;

            return damageType.IsDefenseDamageType();
        }

        public static bool TryGetDefensePurityIncubationStatType(
            CombatDamageType damageType,
            out IncubationStatType incubationStatType)
        {
            incubationStatType = IncubationStatType.Invalid;

            if (!damageType.IsDefenseDamageType())
                return false;

            return Enum.TryParse($"{damageType}{DefensePuritySuffix}", out incubationStatType) &&
                   incubationStatType != IncubationStatType.Invalid;
        }

        public static IEnumerable<IncubationStatType> GetDefensePurityIncubationStatTypes()
        {
            foreach (var damageType in CombatDamageTypes.GetDefenseDamageTypes())
            {
                if (TryGetDefensePurityIncubationStatType(damageType, out var incubationStatType))
                    yield return incubationStatType;
            }
        }

        public static bool TryGetResistanceType(
            IncubationStatType incubationStatType,
            out ResistanceType resistanceType)
        {
            resistanceType = ResistanceType.Invalid;

            var name = incubationStatType.ToString();
            if (!name.EndsWith(ResistancePuritySuffix))
                return false;

            var resistanceName = name[..^ResistancePuritySuffix.Length];
            if (!Enum.TryParse(resistanceName, out resistanceType))
                return false;

            return Resistance.IsValidResistanceType(resistanceType);
        }

        public static bool TryGetResistancePurityIncubationStatType(
            ResistanceType resistanceType,
            out IncubationStatType incubationStatType)
        {
            incubationStatType = IncubationStatType.Invalid;

            if (!Resistance.IsValidResistanceType(resistanceType))
                return false;

            return Enum.TryParse($"{resistanceType}{ResistancePuritySuffix}", out incubationStatType) &&
                   incubationStatType != IncubationStatType.Invalid;
        }

        public static IEnumerable<IncubationStatType> GetResistancePurityIncubationStatTypes()
        {
            foreach (var resistanceType in Resistance.GetAllResistanceTypes())
            {
                if (TryGetResistancePurityIncubationStatType(resistanceType, out var incubationStatType))
                    yield return incubationStatType;
            }
        }

        public static int CalculateDefenseBonus(BeastLevel level, Beast beast, CombatDamageType damageType)
        {
            return CalculateBonus(
                GetValue(level.MaxDefenseBonuses, damageType),
                GetValue(beast.DefensePurities, damageType));
        }

        public static int CalculateResistanceBonus(BeastLevel level, Beast beast, ResistanceType resistanceType)
        {
            return CalculateBonus(
                GetMaxResistanceBonus(level, resistanceType),
                GetResistancePurity(beast, resistanceType));
        }

        public static int GetResistancePurity(Beast beast, ResistanceType resistanceType)
        {
            if (beast.ResistancePurities != null &&
                beast.ResistancePurities.TryGetValue(resistanceType, out var purity))
            {
                return purity;
            }

            return 0;
        }

        public static int GetDefensePurity(Beast beast, CombatDamageType damageType)
        {
            if (beast.DefensePurities != null &&
                beast.DefensePurities.TryGetValue(damageType, out var purity))
            {
                return purity;
            }

            return 0;
        }

        public static int GetDefensePurity(IncubationJob job, CombatDamageType damageType)
        {
            if (job.DefensePurities != null &&
                job.DefensePurities.TryGetValue(damageType, out var purity))
            {
                return purity;
            }

            return 0;
        }

        public static int GetMaxResistanceBonus(BeastLevel level, ResistanceType resistanceType)
        {
            if (level.MaxResistanceBonuses != null &&
                level.MaxResistanceBonuses.TryGetValue(resistanceType, out var bonus))
            {
                return bonus;
            }

            return 0;
        }

        private static int CalculateBonus(int max, int purity)
        {
            return (int)(max * (purity * 0.01f));
        }

        private static int GetValue<TKey>(IReadOnlyDictionary<TKey, int> values, TKey key)
        {
            return values != null && values.TryGetValue(key, out var value)
                ? value
                : 0;
        }
    }
}
