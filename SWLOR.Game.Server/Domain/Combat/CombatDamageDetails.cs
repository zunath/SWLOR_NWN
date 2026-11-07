using SWLOR.Game.Server.Service.CombatService;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;
using NativeDamageType = NWN.Native.API.DamageType;
using NWNScriptDamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The details a <see cref="CombatDamageAttribute"/> gives a <see cref="CombatDamageType"/>, as an immutable value, so the
    /// table <c>CombatDamageTypeExtensions.GetDetails</c> reads is a <c>static readonly</c> field of a deeply immutable type.
    /// </summary>
    public sealed record CombatDamageDetails(
        CombatDamageCategoryType Category,
        CombatDamageType DefenseDamageType,
        AbilityType DefenseAbility,
        ResistanceType ElementalResistanceType,
        ResistanceType SourceResistanceType,
        NWNScriptDamageType NWScriptDamageType,
        NativeDamageType NativeDamageType)
    {
        /// <summary>The same values the attribute holds.</summary>
        public static CombatDamageDetails From(CombatDamageAttribute attribute) =>
            new(
                attribute.Category,
                attribute.DefenseDamageType,
                attribute.DefenseAbility,
                attribute.ElementalResistanceType,
                attribute.SourceResistanceType,
                attribute.NWScriptDamageType,
                attribute.NativeDamageType);
    }
}
