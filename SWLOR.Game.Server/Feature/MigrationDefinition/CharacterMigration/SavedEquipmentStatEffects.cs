using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data counterpart of <see cref="Feature.EquipmentStats"/> for a player: an equipped item's stats
/// counted on the character's record. Each case mirrors the matching EquipmentStats handler. It reads the game's
/// stat rules, so it is content that the saved character (Foundation) calls.
/// </summary>
internal static class SavedEquipmentStatEffects
{
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
                if (record.FP > SavedEquipmentStats.GetMaxFP(character)) record.FP = SavedEquipmentStats.GetMaxFP(character);
                if (record.FP < 0) record.FP = 0;
                break;
            case ItemPropertyType.FPRegen:
                Stat.AdjustFPRegen(record, signed);
                break;
            case ItemPropertyType.Stamina:
                record.MaxStamina += signed;
                if (record.Stamina > SavedEquipmentStats.GetMaxStamina(character)) record.Stamina = SavedEquipmentStats.GetMaxStamina(character);
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
}
