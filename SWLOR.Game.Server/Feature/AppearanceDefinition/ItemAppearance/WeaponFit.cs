using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>Applies shared grip measurements while retaining each item's authored visual transforms.</summary>
public static class WeaponFit
{
    private const ObjectVisualTransformDataScopeType Scope = ObjectVisualTransformDataScopeType.Base;
    private const string AppliedX = "WEAPON_GRIP_FIT_X";
    private const string AppliedY = "WEAPON_GRIP_FIT_Y";
    private const string AppliedZ = "WEAPON_GRIP_FIT_Z";
    private const string AppliedItem = "WEAPON_GRIP_FIT_ITEM";
    private const string HeldRight = "WEAPON_GRIP_FIT_RIGHT";
    private const string HeldLeft = "WEAPON_GRIP_FIT_LEFT";

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnModuleEnter() => RefreshInventory(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnPlayerSpawn)]
    [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
    [NWNEventHandler(ScriptName.OnDroidSpawn)]
    public static void OnSpawn() => RefreshInventory(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire() => NormalizeCurrentItem(GetModuleItemAcquired());

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnUnacquire() => NormalizeCurrentItem(GetModuleItemLost());

    [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
    public static void OnEquipBefore()
    {
        Release(GetItemInSlot(InventorySlot.RightHand, OBJECT_SELF));
        Release(GetItemInSlot(InventorySlot.LeftHand, OBJECT_SELF));
    }

    [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
    public static void OnUnequipBefore() => Release(StringToObject(EventsPlugin.GetEventData("ITEM")));

    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnEquipmentChanged() => RefreshInventory(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleEquip)]
    public static void OnModuleEquip() => Apply(GetPCItemLastEquippedBy());

    [NWNEventHandler(ScriptName.OnModuleUnequip)]
    public static void OnModuleUnequip()
    {
        NormalizeCurrentItem(GetPCItemLastUnequipped());
        Apply(GetPCItemLastUnequippedBy());
    }

    public static void RefreshInventory(uint creature)
    {
        if (!IsCreature(creature)) return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            NormalizeItemAndContents(item, right, left);
        Apply(creature);
    }

    private static void NormalizeCurrentItem(uint item)
    {
        if (!GetIsObjectValid(item)) return;
        var owner = GetItemPossessor(item);
        var creature = IsCreature(owner);
        NormalizeItemAndContents(item,
            creature ? GetItemInSlot(InventorySlot.RightHand, owner) : OBJECT_INVALID,
            creature ? GetItemInSlot(InventorySlot.LeftHand, owner) : OBJECT_INVALID);
        if (creature) Apply(owner);
    }

    private static void NormalizeItemAndContents(uint item, uint right, uint left)
    {
        if (item != right && item != left) Release(item);
        if (!GetHasInventory(item)) return;
        for (var child = GetFirstItemInInventory(item); GetIsObjectValid(child); child = GetNextItemInInventory(item))
            NormalizeItemAndContents(child, right, left);
    }

    public static void Apply(uint creature)
    {
        if (!IsCreature(creature)) return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        ReleaseDisplaced(creature, HeldRight, right, left);
        ReleaseDisplaced(creature, HeldLeft, right, left);
        var rightHand = GetHandModel(creature, CreaturePart.RightHand, "r");
        var leftHand = GetHandModel(creature, CreaturePart.LeftHand, "l");
        ApplyHand(creature, right, rightHand, leftHand, HeldRight);
        ApplyHand(creature, left, leftHand, rightHand, HeldLeft);
    }

    private static bool IsCreature(uint creature) =>
        GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature;

    private static void ReleaseDisplaced(uint creature, string variable, uint right, uint left)
    {
        var previous = GetLocalObject(creature, variable);
        DeleteLocalObject(creature, variable);
        if (GetIsObjectValid(previous) && previous != right && previous != left) NormalizeCurrentItem(previous);
    }

    private static string GetHandModel(uint creature, CreaturePart part, string side)
    {
        if (RobeModelRenderer.GetBasePhenotype(creature) != 0) return string.Empty;
        var armor = GetItemInSlot(InventorySlot.Chest, creature);
        var armorPart = GetIsObjectValid(armor)
            ? GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part) : 0;
        var robe = GetIsObjectValid(armor)
            ? GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe) : 0;
        var appearance = (int)GetAppearanceType(creature);
        if (!Get2DAString("appearance", "MODELTYPE", appearance).StartsWith("P", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        var handHiddenByRobe = robe > 0 &&
            Get2DAString("parts_robe", "HIDEHAND" + side.ToUpperInvariant(), robe) != "0";
        return WeaponGripFits.ResolveHandModel(Get2DAString("appearance", "RACE", appearance),
            GetGender(creature), side, GetCreatureBodyPart(part, creature), armorPart, handHiddenByRobe);
    }

    private static void ApplyHand(uint creature, uint item, string hand, string supportHand, string variable)
    {
        if (!GetIsObjectValid(item)) return;
        var fit = WeaponGripFits.GetFit(GetBaseItemType(item), GetPart(item, AppearanceWeapon.Middle),
            GetPart(item, AppearanceWeapon.Top), GetPart(item, AppearanceWeapon.Bottom), hand, supportHand);
        ApplyOffset(item, fit);
        if (fit != FitVector.Zero) SetLocalObject(creature, variable, item);
    }

    private static int GetPart(uint item, AppearanceWeapon part) =>
        GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)part) * 100 +
        GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)part);

    private static void Release(uint item)
    {
        if (GetIsObjectValid(item) && GetObjectType(item) == ObjectType.Item) ApplyOffset(item, FitVector.Zero);
    }

    private static FitVector RebaseOffset(FitVector current, FitVector previous, FitVector next) =>
        current - previous + next;

    private static FitVector ResolveOwnedOffset(string fittedItem, string currentItem, FitVector offset) =>
        !string.IsNullOrEmpty(fittedItem) && fittedItem == currentItem ? offset : FitVector.Zero;

    private static void ApplyOffset(uint item, FitVector offset)
    {
        var saved = new FitVector(GetLocalFloat(item, AppliedX), GetLocalFloat(item, AppliedY), GetLocalFloat(item, AppliedZ));
        if (saved == FitVector.Zero && offset == FitVector.Zero) return;
        // CopyItem copies locals but resets transforms; an ordinary reload retains the UUID and both values.
        var identity = GetObjectUUID(item);
        var previous = ResolveOwnedOffset(GetLocalString(item, AppliedItem), identity, saved);
        var current = new FitVector(GetObjectVisualTransform(item, ObjectVisualTransform.TranslateX, nScope: Scope),
            GetObjectVisualTransform(item, ObjectVisualTransform.TranslateY, nScope: Scope),
            GetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ, nScope: Scope));
        var next = RebaseOffset(current, previous, offset);
        SetAxis(item, ObjectVisualTransform.TranslateX, current.X, next.X, AppliedX, offset.X);
        SetAxis(item, ObjectVisualTransform.TranslateY, current.Y, next.Y, AppliedY, offset.Y);
        SetAxis(item, ObjectVisualTransform.TranslateZ, current.Z, next.Z, AppliedZ, offset.Z);
        if (offset == FitVector.Zero) DeleteLocalString(item, AppliedItem);
        else SetLocalString(item, AppliedItem, identity);
    }

    private static void SetAxis(uint item, ObjectVisualTransform axis, float current, float next, string variable, float offset)
    {
        if (Math.Abs(current - next) > .000001f) SetObjectVisualTransform(item, axis, next, nScope: Scope);
        if (offset == 0f) DeleteLocalFloat(item, variable);
        else SetLocalFloat(item, variable, offset);
    }
}
