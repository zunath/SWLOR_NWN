using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service;

/// <summary>Validates the simulated companion without treating its native carrier as a melee weapon.</summary>
public static class TwinPistolEquipment
{
    public static bool IsPair(BaseItem? mainHandType, BaseItem? offHandType) =>
        (mainHandType is BaseItem.Pistol or BaseItem.Sling) &&
        offHandType == BaseItem.CompanionPistol;

    public static string GetPairCompatibilityError(BaseItem? mainHandType, BaseItem? offHandType) =>
        mainHandType == BaseItem.LegacyPistol && offHandType == BaseItem.CompanionPistol
            ? "Legacy pistols cannot be paired with a companion pistol." : string.Empty;

    public static (uint MainHand, uint OffHand) GetProspectiveHands(uint creature, uint item, InventorySlot slot)
    {
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (slot == InventorySlot.RightHand) main = item;
        if (slot == InventorySlot.LeftHand) off = item;
        return (main, off);
    }

    public static bool IsActivePair(uint creature)
    {
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!GetIsObjectValid(main) || !GetIsObjectValid(off) ||
            !IsPair(GetBaseItemType(main), GetBaseItemType(off)) || Droid.IsDroid(creature))
            return false;

        if (!string.IsNullOrEmpty(GetCompanionPropertyError(off))) return false;
        return string.IsNullOrEmpty(Item.GetCreatureItemUseError(creature, main, SkillType.TwinPistol)) &&
               string.IsNullOrEmpty(Item.GetCreatureItemUseError(creature, off));
    }

    /// <summary>The simulated hand, available only when both equipped items satisfy the pair's requirements.</summary>
    public static uint GetCompanionWeapon(uint creature) => IsActivePair(creature)
        ? GetItemInSlot(InventorySlot.LeftHand, creature)
        : OBJECT_INVALID;

    /// <summary>Only a paired main-hand pistol's stored Pistol requirement is reinterpreted.</summary>
    public static SkillType ResolveRequirementSkill(BaseItem itemType, SkillType requirement, bool paired) =>
        paired && requirement == SkillType.Pistol && IsPair(itemType, BaseItem.CompanionPistol)
            ? SkillType.TwinPistol
            : requirement;

    /// <summary>Pure classification for an already validated arrangement; explicit ability identities win.</summary>
    public static SkillType ResolveSkill(BaseItem weaponType, BaseItem? mainHandType, BaseItem? offHandType,
        SkillType? explicitOverride = null)
    {
        if (explicitOverride.HasValue && explicitOverride != SkillType.Invalid) return explicitOverride.Value;
        if (IsPair(mainHandType, offHandType) &&
            (weaponType == mainHandType || weaponType == BaseItem.CompanionPistol)) return SkillType.TwinPistol;
        return weaponType == BaseItem.CompanionPistol ? SkillType.Invalid : Skill.GetSkillTypeByBaseItem(weaponType);
    }

    /// <summary>Companion properties are profile data only; no native or creature-wide equipped bonuses.</summary>
    public static bool IsSupportedCompanionProperty(ItemPropertyType type) => type is
        ItemPropertyType.DMG or ItemPropertyType.Delay or ItemPropertyType.Accuracy or
        ItemPropertyType.AccuracyStat or ItemPropertyType.DamageStat or ItemPropertyType.WeaponDamageType or
        ItemPropertyType.RequiresSkill or ItemPropertyType.RequiresStat or ItemPropertyType.UseLimitationPerk or
        ItemPropertyType.WeaponEnhancement or ItemPropertyType.EnhancementLevel or
        ItemPropertyType.IncreasedPrice or ItemPropertyType.ProgressPenalty or ItemPropertyType.Blueprint;

    public static string GetCompanionPropertyError(uint item)
    {
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            if (!IsSupportedCompanionProperty(GetItemPropertyType(ip)))
                return "Companion pistols cannot use native or creature-wide equipment bonuses.";
        return string.Empty;
    }
}
