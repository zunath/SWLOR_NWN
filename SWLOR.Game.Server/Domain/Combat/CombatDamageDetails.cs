using SWLOR.Game.Server.Domain.Combat;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;
using NativeDamageType = NWN.Native.API.DamageType;
using NWNScriptDamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The details a <see cref="CombatDamageAttribute"/> gives a <see cref="CombatDamageType"/>, as an immutable value, so the
    /// table <c>CombatDamageTypeExtensions.GetDetails</c> reads is a <c>static readonly</c> field of a deeply immutable type.
    /// The engine's own damage type is kept as its number (<see cref="NativeDamageTypeValue"/>): the static-state rule
    /// accepts the enums of the server and of <c>SWLOR.NWN.API</c>, and <see cref="NativeDamageType"/> is an enum of
    /// <c>NWN.Native</c>.
    /// </summary>
    public sealed record CombatDamageDetails(
        CombatDamageCategoryType Category,
        CombatDamageType DefenseDamageType,
        AbilityType DefenseAbility,
        ResistanceType ElementalResistanceType,
        ResistanceType SourceResistanceType,
        NWNScriptDamageType NWScriptDamageType,
        int NativeDamageTypeValue)
    {
        /// <summary>The engine's damage type for this combat damage type.</summary>
        public NativeDamageType NativeDamageType => (NativeDamageType)NativeDamageTypeValue;

        /// <summary>The same values the attribute holds.</summary>
        public static CombatDamageDetails From(CombatDamageAttribute attribute) =>
            new(
                attribute.Category,
                attribute.DefenseDamageType,
                attribute.DefenseAbility,
                attribute.ElementalResistanceType,
                attribute.SourceResistanceType,
                attribute.NWScriptDamageType,
                (int)attribute.NativeDamageType);
    }
}
