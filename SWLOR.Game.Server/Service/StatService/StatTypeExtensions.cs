using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Service.StatService
{
    public static class StatTypeExtensions
    {
        private static readonly Dictionary<StatType, StatTypeCategory> Categories = Enum
            .GetValues(typeof(StatType))
            .Cast<StatType>()
            .ToDictionary(statType => statType, GetMetadataCategory);

        public static StatTypeCategory GetCategory(this StatType statType)
        {
            return Categories.TryGetValue(statType, out var category)
                ? category
                : StatTypeCategory.NonBeneficial;
        }

        public static bool IsBeneficialAdjustment(this StatType statType, int value)
        {
            if (value == 0)
                return false;

            return statType.GetCategory() switch
            {
                StatTypeCategory.BeneficialWhenPositive => value > 0,
                StatTypeCategory.BeneficialWhenNegative => value < 0,
                _ => false
            };
        }

        private static StatTypeCategory GetMetadataCategory(StatType statType)
        {
            var member = typeof(StatType).GetMember(statType.ToString()).FirstOrDefault();
            var metadata = member?.GetCustomAttribute<StatTypeAttribute>();

            return metadata?.Category ?? StatTypeCategory.NonBeneficial;
        }
    }
}
