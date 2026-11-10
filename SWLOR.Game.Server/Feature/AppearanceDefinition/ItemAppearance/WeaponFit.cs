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
    private const string AppliedRotationX = "WEAPON_GRIP_FIT_RX";
    private const string AppliedRotationY = "WEAPON_GRIP_FIT_RY";
    private const string AppliedRotationZ = "WEAPON_GRIP_FIT_RZ";
    private const string AppliedScale = "WEAPON_GRIP_FIT_SCALE";
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
        WeaponGripHandRenderer.Apply(creature);
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        ReleaseDisplaced(creature, HeldRight, right, left);
        ReleaseDisplaced(creature, HeldLeft, right, left);
        var rightHand = GetHandModel(creature, CreaturePart.RightHand, "r");
        var leftHand = GetHandModel(creature, CreaturePart.LeftHand, "l");
        ApplyHand(creature, right, rightHand, leftHand, HeldRight,
            RifleGripFits.IsReadyAnimation(ObjectPlugin.GetCurrentAnimation(creature)));
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
        return WeaponGripHandRenderer.GetHandModel(creature, part, side);
    }

    internal static void ApplyRifleAnimation(uint creature, int animation)
    {
        if (!IsCreature(creature)) return;
        var item = GetItemInSlot(InventorySlot.RightHand, creature);
        if (!GetIsObjectValid(item) || GetBaseItemType(item) != BaseItem.Rifle) return;
        ApplyHand(creature, item, GetHandModel(creature, CreaturePart.RightHand, "r"),
            GetHandModel(creature, CreaturePart.LeftHand, "l"), HeldRight,
            RifleGripFits.IsReadyAnimation(animation));
    }

    private static void ApplyHand(uint creature, uint item, string hand, string supportHand, string variable,
        bool rifleReady = false)
    {
        if (!GetIsObjectValid(item)) return;
        var fit = WeaponGripFits.GetPlacement(GetBaseItemType(item), GetPart(item, AppearanceWeapon.Middle),
            GetPart(item, AppearanceWeapon.Top), GetPart(item, AppearanceWeapon.Bottom), hand, supportHand);
        if (GetBaseItemType(item) == BaseItem.Rifle && GetPart(item, AppearanceWeapon.Top) == 101 &&
            GetPart(item, AppearanceWeapon.Bottom) == 101)
            RifleGripFits.TryGetPosePlacement(GetPart(item, AppearanceWeapon.Middle), hand, supportHand,
                rifleReady, out fit);
        ApplyPlacement(item, fit);
        if (fit != WeaponGripFit.Identity) SetLocalObject(creature, variable, item);
    }

    internal static int GetPart(uint item, AppearanceWeapon part) =>
        GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)part) * 100 +
        GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)part);

    private static void Release(uint item)
    {
        if (GetIsObjectValid(item) && GetObjectType(item) == ObjectType.Item) ApplyPlacement(item, WeaponGripFit.Identity);
    }

    private static FitVector RebaseOffset(FitVector current, FitVector previous, FitVector next) =>
        current - previous + next;

    private static FitVector ResolveOwnedOffset(string fittedItem, string currentItem, FitVector offset) =>
        !string.IsNullOrEmpty(fittedItem) && fittedItem == currentItem ? offset : FitVector.Zero;

    private static float RebaseScale(float current, float previous, float next) => current / previous * next;

    private static void ApplyPlacement(uint item, WeaponGripFit fit)
    {
        var saved = new FitVector(GetLocalFloat(item, AppliedX), GetLocalFloat(item, AppliedY), GetLocalFloat(item, AppliedZ));
        var savedRotation = new FitVector(GetLocalFloat(item, AppliedRotationX), GetLocalFloat(item, AppliedRotationY),
            GetLocalFloat(item, AppliedRotationZ));
        var savedScale = GetLocalFloat(item, AppliedScale);
        // Translation-only fits from earlier versions did not store a scale.
        if (savedScale <= 0f) savedScale = 1f;
        if (saved == FitVector.Zero && savedRotation == FitVector.Zero && savedScale == 1f &&
            fit == WeaponGripFit.Identity) return;
        // CopyItem copies locals but resets transforms; an ordinary reload retains the UUID and both values.
        var identity = GetObjectUUID(item);
        var fittedItem = GetLocalString(item, AppliedItem);
        var previous = ResolveOwnedOffset(fittedItem, identity, saved);
        var previousRotation = ResolveOwnedOffset(fittedItem, identity, savedRotation);
        var previousScale = !string.IsNullOrEmpty(fittedItem) && fittedItem == identity ? savedScale : 1f;
        var current = new FitVector(GetObjectVisualTransform(item, ObjectVisualTransform.TranslateX, nScope: Scope),
            GetObjectVisualTransform(item, ObjectVisualTransform.TranslateY, nScope: Scope),
            GetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ, nScope: Scope));
        var next = RebaseOffset(current, previous, fit.Translation);
        SetAxis(item, ObjectVisualTransform.TranslateX, current.X, next.X, AppliedX, fit.Translation.X);
        SetAxis(item, ObjectVisualTransform.TranslateY, current.Y, next.Y, AppliedY, fit.Translation.Y);
        SetAxis(item, ObjectVisualTransform.TranslateZ, current.Z, next.Z, AppliedZ, fit.Translation.Z);
        var rotation = new FitVector(GetObjectVisualTransform(item, ObjectVisualTransform.RotateX, nScope: Scope),
            GetObjectVisualTransform(item, ObjectVisualTransform.RotateY, nScope: Scope),
            GetObjectVisualTransform(item, ObjectVisualTransform.RotateZ, nScope: Scope));
        var nextRotation = RebaseOffset(rotation, previousRotation, fit.Rotation);
        SetAxis(item, ObjectVisualTransform.RotateX, rotation.X, nextRotation.X, AppliedRotationX, fit.Rotation.X);
        SetAxis(item, ObjectVisualTransform.RotateY, rotation.Y, nextRotation.Y, AppliedRotationY, fit.Rotation.Y);
        SetAxis(item, ObjectVisualTransform.RotateZ, rotation.Z, nextRotation.Z, AppliedRotationZ, fit.Rotation.Z);
        var scale = GetObjectVisualTransform(item, ObjectVisualTransform.Scale, nScope: Scope);
        var nextScale = RebaseScale(scale, previousScale, fit.Scale);
        if (Math.Abs(scale - nextScale) > .000001f)
            SetObjectVisualTransform(item, ObjectVisualTransform.Scale, nextScale, nScope: Scope);
        if (fit.Scale == 1f) DeleteLocalFloat(item, AppliedScale);
        else SetLocalFloat(item, AppliedScale, fit.Scale);
        if (fit == WeaponGripFit.Identity) DeleteLocalString(item, AppliedItem);
        else SetLocalString(item, AppliedItem, identity);
    }

    private static void SetAxis(uint item, ObjectVisualTransform axis, float current, float next, string variable, float offset)
    {
        if (Math.Abs(current - next) > .000001f) SetObjectVisualTransform(item, axis, next, nScope: Scope);
        if (offset == 0f) DeleteLocalFloat(item, variable);
        else SetLocalFloat(item, variable, offset);
    }
}
