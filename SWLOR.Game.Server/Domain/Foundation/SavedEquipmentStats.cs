using System.Collections.Immutable;
using System;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Foundation;

/// <summary>
/// The saved-data totals a character's record derives from its equipped items (pools and ability scores). The per-item
/// stat application is <c>SavedEquipmentStatEffects</c>, which reads the game's stat rules.
/// </summary>
internal static class SavedEquipmentStats
{
    private const int FPPerWillpower = 3;
    private const int StaminaPerTwoMight = 3;
    private const int MaximumItemAbilityBonus = 12;

    /// <summary>
    /// <see cref="Domain.Character.IStatService.GetMaxFP(uint, Entity.Player)"/> from saved data: the record's base, plus Willpower from
    /// the saved score and equipped ability bonuses. Status-effect adjustments only exist on a live character.
    /// </summary>
    public static int GetMaxFP(SavedCharacter character) =>
        character.Record.MaxFP + GetAbilityScore(character, AbilityType.Willpower) * FPPerWillpower;

    public static int GetMaxStamina(SavedCharacter character) =>
        character.Record.MaxStamina + GetAbilityScore(character, AbilityType.Might) * StaminaPerTwoMight / 2;

    private static readonly ImmutableArray<string> AbilityFields = ImmutableArray.Create<string>("Str", "Dex", "Con", "Int", "Wis", "Cha");

    public static int GetAbilityScore(SavedCharacter character, AbilityType ability)
    {
        var bonus = character.Equipped.Sum(item => item.Properties
            .Where(property => property.Type == ItemPropertyType.AbilityBonus && property.Subtype == (int)ability)
            .Sum(property => property.CostValue));
        return character.ReadInteger(AbilityFields[(int)ability]) + Math.Min(bonus, MaximumItemAbilityBonus);
    }
}
