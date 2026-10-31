using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

public static class SlitherhornFit
{
    private const ObjectVisualTransformDataScopeType FitScope =
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2;
    private const string AppliedOffsetVariable = "SLITHERHORN_FIT_Z";
    private const string FittedItemVariable = "SLITHERHORN_FIT_ITEM";

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnModuleEnter() => RefreshInventory(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnPlayerSpawn)]
    [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
    [NWNEventHandler(ScriptName.OnDroidSpawn)]
    public static void OnSpawn() => RefreshInventory(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var item = GetModuleItemAcquired();
        NormalizeCurrentItem(item);
        Apply(GetItemPossessor(item));
    }

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
        Release(GetPCItemLastUnequipped());
        Apply(GetPCItemLastUnequippedBy());
    }

    public static void RefreshInventory(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;

        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            NormalizeItemAndContents(item, right);
        Apply(creature);
    }

    private static void NormalizeCurrentItem(uint item)
    {
        if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
            return;
        var owner = GetItemPossessor(item);
        NormalizeItemAndContents(item, GetObjectType(owner) == ObjectType.Creature
            ? GetItemInSlot(InventorySlot.RightHand, owner) : OBJECT_INVALID);
    }

    private static void NormalizeItemAndContents(uint item, uint right)
    {
        if (!GetIsObjectValid(item))
            return;
        if (item != right)
            Release(item);
        if (!GetHasInventory(item))
            return;
        for (var child = GetFirstItemInInventory(item); GetIsObjectValid(child); child = GetNextItemInInventory(item))
            NormalizeItemAndContents(child, right);
    }

    public static void Apply(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;

        Release(GetItemInSlot(InventorySlot.LeftHand, creature));
        var item = GetItemInSlot(InventorySlot.RightHand, creature);
        if (!GetIsObjectValid(item))
            return;
        ApplyOffset(item, ResolveOffset(GetResRef(item), GetAppearanceType(creature),
            RobeModelRenderer.GetBasePhenotype(creature), GetGender(creature)));
    }

    private static float ResolveOffset(string resref, AppearanceType appearance, int phenotype, Gender gender)
    {
        // Other body families need their own measured attachment profiles.
        if (!string.Equals(resref, "slitherhorn", StringComparison.OrdinalIgnoreCase) ||
            appearance != AppearanceType.Human || phenotype != 0)
            return 0f;
        return gender switch
        {
            Gender.Male => .035f,
            Gender.Female => -.028f,
            _ => 0f
        };
    }

    private static void Release(uint item)
    {
        if (GetIsObjectValid(item) && GetObjectType(item) == ObjectType.Item)
            ApplyOffset(item, 0f);
    }

    private static float RebaseOffset(float current, float previousOffset, float nextOffset) =>
        current - previousOffset + nextOffset;

    private static float ResolveOwnedOffset(string fittedItem, string currentItem, float offset) =>
        !string.IsNullOrEmpty(fittedItem) && fittedItem == currentItem ? offset : 0f;

    private static void ApplyOffset(uint item, float offset)
    {
        var saved = GetLocalFloat(item, AppliedOffsetVariable);
        if (saved == 0f && offset == 0f)
            return;
        // CopyItem copies locals but resets visual transforms; UUIDs survive ordinary reloads.
        var identity = GetObjectUUID(item);
        var previous = ResolveOwnedOffset(GetLocalString(item, FittedItemVariable), identity, saved);
        var current = GetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ, nScope: FitScope);
        var next = RebaseOffset(current, previous, offset);
        if (Math.Abs(current - next) > .000001f)
            SetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ, next, nScope: FitScope);
        // Both values persist on the item, so reloading cannot compound the fit.
        if (offset == 0f)
        {
            DeleteLocalFloat(item, AppliedOffsetVariable);
            DeleteLocalString(item, FittedItemVariable);
        }
        else
        {
            SetLocalFloat(item, AppliedOffsetVariable, offset);
            SetLocalString(item, FittedItemVariable, identity);
        }
    }
}
