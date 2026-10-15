using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Maps each registered ability definition back to the feat that activates it, so delayed
    /// pulses and fallback attribution can name the feat without the original cast.
    /// </summary>
    public static class CombatRecordAbilityFeatIndex
    {
        /// <summary>
        /// A definition registered under more than one feat maps to <see cref="FeatType.Invalid"/>,
        /// since the feat cannot be told from the definition alone.
        /// </summary>
        public static Dictionary<AbilityDetail, FeatType> Build(IReadOnlyDictionary<FeatType, AbilityDetail> abilities)
        {
            var index = new Dictionary<AbilityDetail, FeatType>(ReferenceEqualityComparer.Instance);
            foreach (var (feat, ability) in abilities)
            {
                if (ability == null)
                    continue;

                index[ability] = index.ContainsKey(ability) ? FeatType.Invalid : feat;
            }

            return index;
        }
    }
}
