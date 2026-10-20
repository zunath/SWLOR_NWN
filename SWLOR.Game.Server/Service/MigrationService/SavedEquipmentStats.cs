using System;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// Saved-data counterpart of <see cref="Feature.EquipmentStats"/> for a player: an equipped item's stats
/// counted on the character's record. Each case mirrors the matching EquipmentStats handler.
/// </summary>
internal static class SavedEquipmentStats
{
    private const int FPPerWillpower = 3;
    private const int StaminaPerTwoMight = 3;
    private const int MaximumItemAbilityBonus = 12;

    public static void Apply(SavedCharacter character, SavedItem item, ItemPropertyType type, int subtype, int amount, bool isAdding)
    {
        var record = character.Record;
        var signed = isAdding ? amount : -amount;
        switch (type)
        {
            case ItemPropertyType.HPBonus:
                character.AdjustPlayerMaxHP(signed);
                break;
            case ItemPropertyType.FP:
                record.MaxFP += signed;
                if (record.FP > GetMaxFP(character)) record.FP = GetMaxFP(character);
                if (record.FP < 0) record.FP = 0;
                break;
            case ItemPropertyType.FPRegen:
                Stat.AdjustFPRegen(record, signed);
                break;
            case ItemPropertyType.Stamina:
                record.MaxStamina += signed;
                if (record.Stamina > GetMaxStamina(character)) record.Stamina = GetMaxStamina(character);
                if (record.Stamina < 0) record.Stamina = 0;
                break;
            case ItemPropertyType.STMRegen:
                Stat.AdjustSTMRegen(record, signed);
                break;
            case ItemPropertyType.CombatReadiness:
                // Player totals are refreshed from the equipped items after the change.
                break;
            case ItemPropertyType.Attack:
                Stat.AdjustAttack(record, signed);
                break;
            case ItemPropertyType.Accuracy:
                if (Item.IsAttackWeaponType(item.BaseItem) || item.BaseItem == BaseItem.CreatureItem)
                    break;
                Stat.AdjustAccuracy(record, signed);
                break;
            case ItemPropertyType.ForceAttack:
                Stat.AdjustForceAttack(record, signed);
                break;
            case ItemPropertyType.Defense:
                if (((CombatDamageType)subtype).IsDefenseDamageType())
                    Stat.AdjustDefense(record, (CombatDamageType)subtype, signed);
                break;
            case ItemPropertyType.Resistance:
            {
                var resistance = Resistance.DecodeItemPropertyCostTableValue(amount);
                if (Resistance.IsValidResistanceType((ResistanceType)subtype))
                    Resistance.AdjustResistance(record, (ResistanceType)subtype, isAdding ? resistance : -resistance);
                break;
            }
            case ItemPropertyType.Evasion:
                Stat.AdjustEvasion(record, signed);
                break;
            case ItemPropertyType.Control:
            {
                var skill = GetCraftSkill(subtype);
                if (skill != SkillType.Invalid)
                    Stat.AdjustControl(record, skill, signed);
                break;
            }
            case ItemPropertyType.Craftsmanship:
                Stat.AdjustCraftsmanship(record, GetCraftSkill(subtype), signed);
                break;
            case ItemPropertyType.CPBonus:
                Stat.AdjustCPBonus(record, GetCraftSkill(subtype), signed);
                break;
            case ItemPropertyType.Stealth:
                Stat.AdjustStealth(record, signed);
                break;
            case ItemPropertyType.Detection:
                Stat.AdjustDetection(record, signed);
                break;
            case ItemPropertyType.TrapBonus:
                Stat.AdjustTrapBonus(record, signed);
                break;
            case ItemPropertyType.Disarm:
                Stat.AdjustTrapDisarm(record, signed);
                break;
            case ItemPropertyType.PoisonBonus:
                Stat.AdjustPoisonBonus(record, signed);
                break;
            case ItemPropertyType.Lockpicking:
                Stat.AdjustLockpicking(record, signed);
                break;
        }
    }

    // Types are defined in iprp_crafttype.2da.
    private static SkillType GetCraftSkill(int subtype) => subtype switch
    {
        1 => SkillType.Smithery,
        2 => SkillType.Engineering,
        3 => SkillType.Fabrication,
        4 => SkillType.Agriculture,
        _ => SkillType.Invalid,
    };

    /// <summary>
    /// <see cref="Stat.GetMaxFP(uint, Entity.Player)"/> from saved data: the record's base, plus Willpower from
    /// the saved score and equipped ability bonuses. Status-effect adjustments only exist on a live character.
    /// </summary>
    public static int GetMaxFP(SavedCharacter character) =>
        character.Record.MaxFP + GetAbilityScore(character, AbilityType.Willpower) * FPPerWillpower;

    public static int GetMaxStamina(SavedCharacter character) =>
        character.Record.MaxStamina + GetAbilityScore(character, AbilityType.Might) * StaminaPerTwoMight / 2;

    private static readonly string[] AbilityFields = { "Str", "Dex", "Con", "Int", "Wis", "Cha" };

    public static int GetAbilityScore(SavedCharacter character, AbilityType ability)
    {
        var bonus = character.Equipped.Sum(item => item.Properties
            .Where(property => property.Type == ItemPropertyType.AbilityBonus && property.Subtype == (int)ability)
            .Sum(property => property.CostValue));
        return character.ReadInteger(AbilityFields[(int)ability]) + Math.Min(bonus, MaximumItemAbilityBonus);
    }
}
