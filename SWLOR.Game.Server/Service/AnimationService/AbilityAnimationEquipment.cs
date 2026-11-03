using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>Matches authored grips against native base-item data and both hands, never skill categories.</summary>
public static class AbilityAnimationEquipment
{
    private sealed record WeaponData(int? Wield, int? Size, int? Type);
    private static readonly Dictionary<BaseItem, WeaponData> WeaponRows = new();

    public static bool IsCompatible(AnimationEquipmentRequirement requirement, uint creature)
    {
        if (requirement == AnimationEquipmentRequirement.Unrestricted) return true;
        var size = (int)GetCreatureSize(creature);
        return IsCompatible(requirement, ReadGrip(GetItemInSlot(InventorySlot.RightHand, creature), size),
            ReadGrip(GetItemInSlot(InventorySlot.LeftHand, creature), size));
    }

    public static AnimationWeaponHold ReadHold(uint creature)
    {
        var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
        if (!GetIsObjectValid(weapon)) return AnimationWeaponHold.None;
        return ResolveHold(GetBaseItemType(weapon), ReadGrip(weapon, (int)GetCreatureSize(creature)));
    }

    public static AnimationWeaponHold ResolveHold(BaseItem type, AnimationWeaponGrip grip)
    {
        if (type is BaseItem.Rifle or BaseItem.Cannon) return AnimationWeaponHold.Rifle;
        return grip is AnimationWeaponGrip.TwoHanded or AnimationWeaponGrip.Polearm
            ? AnimationWeaponHold.Shoulder : AnimationWeaponHold.None;
    }

    private static AnimationWeaponGrip ReadGrip(uint item, int creatureSize)
    {
        if (!GetIsObjectValid(item)) return AnimationWeaponGrip.Empty;
        var type = GetBaseItemType(item);
        if (!WeaponRows.TryGetValue(type, out var row))
        {
            int? Read(string column) => int.TryParse(Get2DAString("baseitems", column, (int)type), out var value) ? value : null;
            row = new WeaponData(Read("WeaponWield"), Read("WeaponSize"), Read("WeaponType"));
            WeaponRows[type] = row;
        }
        return ResolveGrip(type, row.Wield, row.Size, row.Type, creatureSize);
    }

    public static AnimationWeaponGrip ResolveGrip(BaseItem type, int? wield, int? size, int? weaponType, int creatureSize)
    {
        if (type == BaseItem.Invalid) return AnimationWeaponGrip.Empty;
        if (type is BaseItem.Katar or BaseItem.PowerGlove or BaseItem.CompanionPowerGlove) return AnimationWeaponGrip.Unarmed;
        if (type == BaseItem.CompanionPistol) return AnimationWeaponGrip.CompanionPistol;
        if (wield == 7) return AnimationWeaponGrip.Shield;
        if (size is not (>= 1 and <= 5) || creatureSize is < 1 or > 5 || size > creatureSize + 1)
            return AnimationWeaponGrip.Unknown;
        return wield switch
        {
            4 or 8 => AnimationWeaponGrip.Polearm,
            6 => AnimationWeaponGrip.Rifle,
            10 => AnimationWeaponGrip.Pistol,
            11 => AnimationWeaponGrip.Throwing,
            // Ordinary melee rows leave WeaponWield blank. Their grip depends on relative weapon size.
            null or 0 when weaponType is >= 1 and <= 4 => size > creatureSize
                ? AnimationWeaponGrip.TwoHanded : AnimationWeaponGrip.OneHanded,
            _ => AnimationWeaponGrip.Unknown
        };
    }

    public static bool IsCompatible(AnimationEquipmentRequirement requirement, AnimationWeaponGrip mainHand, AnimationWeaponGrip offHand) =>
        requirement switch
        {
            AnimationEquipmentRequirement.Unrestricted => true,
            AnimationEquipmentRequirement.OneHanded => mainHand == AnimationWeaponGrip.OneHanded && offHand == AnimationWeaponGrip.Empty,
            AnimationEquipmentRequirement.OneHandedWithOffHand => mainHand == AnimationWeaponGrip.OneHanded &&
                offHand is AnimationWeaponGrip.Empty or AnimationWeaponGrip.OneHanded or AnimationWeaponGrip.Shield,
            AnimationEquipmentRequirement.WeaponAndShield => mainHand == AnimationWeaponGrip.OneHanded && offHand == AnimationWeaponGrip.Shield,
            AnimationEquipmentRequirement.TwoHanded => mainHand == AnimationWeaponGrip.TwoHanded && offHand == AnimationWeaponGrip.Empty,
            AnimationEquipmentRequirement.Polearm => mainHand == AnimationWeaponGrip.Polearm && offHand == AnimationWeaponGrip.Empty,
            AnimationEquipmentRequirement.DualWield => mainHand == AnimationWeaponGrip.OneHanded && offHand == AnimationWeaponGrip.OneHanded,
            AnimationEquipmentRequirement.Unarmed => mainHand is AnimationWeaponGrip.Empty or AnimationWeaponGrip.Unarmed &&
                offHand is AnimationWeaponGrip.Empty or AnimationWeaponGrip.Unarmed,
            AnimationEquipmentRequirement.Pistol => mainHand == AnimationWeaponGrip.Pistol && offHand == AnimationWeaponGrip.Empty,
            AnimationEquipmentRequirement.PistolWithOffHand => mainHand == AnimationWeaponGrip.Pistol &&
                offHand is AnimationWeaponGrip.Empty or AnimationWeaponGrip.Shield or AnimationWeaponGrip.CompanionPistol,
            AnimationEquipmentRequirement.Rifle => mainHand == AnimationWeaponGrip.Rifle && offHand == AnimationWeaponGrip.Empty,
            AnimationEquipmentRequirement.Throwing => mainHand == AnimationWeaponGrip.Throwing && offHand == AnimationWeaponGrip.Empty,
            AnimationEquipmentRequirement.TwinPistol => mainHand == AnimationWeaponGrip.Pistol && offHand == AnimationWeaponGrip.CompanionPistol,
            _ => false
        };
}
