using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// The default defense and resistance tables of the persisted entities (<c>Player</c>, <c>Beast</c>,
    /// <c>IncubationJob</c>) and the one place that decides which damage types those tables hold. Pure: no state, no
    /// engine and no database, so entity constructors and offline tools can call it. <c>CombatRules</c> and
    /// <c>ResistanceRules</c> keep a forwarder for each member that master's code still names through them.
    /// </summary>
    public static class DamageTableDefaults
    {
        // The defense damage types in enum order: the order of the keys of every Defenses dictionary.
        private static readonly ImmutableArray<CombatDamageType> DefenseDamageTypes =
            Enum.GetValues<CombatDamageType>().Where(type => type.IsDefenseDamageType()).ToImmutableArray();

        /// <summary>A new table holding <paramref name="defaultValue"/> for every defense damage type, in enum order.</summary>
        public static Dictionary<CombatDamageType, int> CreateDefaultDefenseValues(int defaultValue = 0)
        {
            return DefenseDamageTypes
                .ToDictionary(type => type, _ => defaultValue);
        }

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
    }
}
