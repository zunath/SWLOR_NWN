using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.ResistanceService
{
    /// <summary>
    /// The pure rules of resistances: limits, how a resistance turns into a damage multiplier, and the item property
    /// cost table encoding. No state, no engine and no database, so code that has no container (entity constructors,
    /// the combat simulator, offline tools) can call it directly. <c>Resistance</c> and the resistance service use it too.
    /// </summary>
    public static class ResistanceRules
    {
        public const int MinimumResistance = -100;
        public const int MaximumResistance = 100;
        public const int VulnerabilityCostTableOffset = MaximumResistance;

        /// <summary>
        /// Every resistance type in enum order, without Invalid. The one place that decides which types exist: the
        /// resistance service builds its tables from it, and so do callers that have no container.
        /// </summary>
        public static IEnumerable<ResistanceType> EnumerateResistanceTypes()
        {
            foreach (var type in Enum.GetValues<ResistanceType>())
            {
                if (type != ResistanceType.Invalid)
                    yield return type;
            }
        }

        /// <summary>A new table holding <paramref name="defaultValue"/> for every resistance type.</summary>
        public static Dictionary<ResistanceType, int> CreateDefaultResistanceValues(int defaultValue = 0)
        {
            return EnumerateResistanceTypes().ToDictionary(type => type, _ => defaultValue);
        }

        public static int ClampResistance(int resistance)
        {
            return Math.Clamp(resistance, MinimumResistance, MaximumResistance);
        }

        public static float CalculateResistanceDamageMultiplier(int resistance)
        {
            resistance = ClampResistance(resistance);

            return Math.Max(0f, 1f - (resistance / 100f));
        }

        public static int EncodeItemPropertyCostTableValue(int resistance)
        {
            resistance = ClampResistance(resistance);
            return resistance < 0
                ? VulnerabilityCostTableOffset + Math.Abs(resistance)
                : resistance;
        }

        public static int DecodeItemPropertyCostTableValue(int costTableValue)
        {
            if (costTableValue < 0)
                return ClampResistance(costTableValue);

            if (costTableValue > VulnerabilityCostTableOffset)
                return ClampResistance(-(costTableValue - VulnerabilityCostTableOffset));

            return ClampResistance(costTableValue);
        }
    }
}
