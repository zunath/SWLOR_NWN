using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>Fits the compact Power Glove shells without changing their authored appearance.</summary>
public static class PowerGloveFit
{
    private const ObjectVisualTransformDataScopeType FitScope =
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2;

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnModuleEnter() => RefreshInventory(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnPlayerSpawn)]
    [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
    [NWNEventHandler(ScriptName.OnDroidSpawn)]
    public static void OnSpawn() => RefreshInventory(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var owner = GetModuleItemAcquiredBy();
        var creature = GetObjectType(owner) == ObjectType.Creature;
        NormalizeItemAndContents(GetModuleItemAcquired(),
            creature ? GetItemInSlot(InventorySlot.RightHand, owner) : OBJECT_INVALID,
            creature ? GetItemInSlot(InventorySlot.LeftHand, owner) : OBJECT_INVALID);
        if (creature)
            Apply(owner);
    }

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnUnacquire() => NormalizeItemAndContents(GetModuleItemLost(), OBJECT_INVALID, OBJECT_INVALID);

    [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
    public static void OnEquipBefore()
    {
        // A replacement need not raise a separate unequip event for the displaced item.
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

    /// <summary>Normalizes loaded inventory, including bags, then fits the current equipment.</summary>
    public static void RefreshInventory(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;

        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            NormalizeItemAndContents(item, right, left);
        Apply(creature);
    }

    private static void NormalizeItemAndContents(uint item, uint right, uint left)
    {
        if (!GetIsObjectValid(item))
            return;
        if (item != right && item != left)
            Release(item);
        if (!GetHasInventory(item))
            return;
        for (var child = GetFirstItemInInventory(item); GetIsObjectValid(child); child = GetNextItemInInventory(item))
            NormalizeItemAndContents(child, right, left);
    }

    /// <summary>Recomputes both attachments from the wearer's current natural or armored hands.</summary>
    public static void Apply(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsPowerGlove(right) && !IsPowerGlove(left))
            return;

        var rightModel = GetHandModel(creature, CreaturePart.RightHand, "r");
        var leftModel = GetHandModel(creature, CreaturePart.LeftHand, "l");
        Fit(right, rightModel, leftModel);
        Fit(left, rightModel, leftModel);
    }

    private static string GetHandModel(uint creature, CreaturePart part, string side)
    {
        var appearance = (int)GetAppearanceType(creature);
        if (!Get2DAString("appearance", "MODELTYPE", appearance).StartsWith("P", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        var armor = GetItemInSlot(InventorySlot.Chest, creature);
        var armorPart = GetIsObjectValid(armor)
            ? GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part)
            : 0;
        return ResolveHandModel(Get2DAString("appearance", "RACE", appearance), GetGender(creature),
            RobeModelRenderer.GetBasePhenotype(creature), side, GetCreatureBodyPart(part, creature), armorPart);
    }

    private static string ResolveHandModel(string race, Gender gender, int phenotype, string side, int naturalPart, int armorPart)
    {
        var part = armorPart > 0 ? armorPart : naturalPart;
        if (string.IsNullOrWhiteSpace(race) || race.Length != 1 || part <= 0)
            return string.Empty;
        return $"p{(gender == Gender.Female ? 'f' : 'm')}{race.ToLowerInvariant()}{phenotype}_hand{side}{part:D3}";
    }

    private static void Fit(uint item, string rightModel, string leftModel)
    {
        if (!IsPowerGlove(item))
            return;
        var appearance = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle) * 100 +
                         GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle);
        SetScale(item, ResolveScale(rightModel, leftModel, appearance));
    }

    private static float ResolveScale(string rightModel, string leftModel, int appearance) =>
        Math.Max(PowerGloveHandFitCatalog.GetScale(rightModel, appearance),
            PowerGloveHandFitCatalog.GetScale(leftModel, appearance));

    private static bool IsPowerGlove(uint item) => GetIsObjectValid(item) &&
        GetObjectType(item) == ObjectType.Item && GetBaseItemType(item) == BaseItem.PowerGlove;

    private static void Release(uint item)
    {
        if (IsPowerGlove(item))
            SetScale(item, 1f);
    }

    private static void SetScale(uint item, float scale)
    {
        // Absolute, part-scoped values cannot compound after export/import or repeated refreshes.
        // The base scale and all rotation/translation channels remain independently owned.
        if (Math.Abs(GetObjectVisualTransform(item, ObjectVisualTransform.Scale, nScope: FitScope) - scale) > 0.0001f)
            SetObjectVisualTransform(item, ObjectVisualTransform.Scale, scale, nScope: FitScope);
    }
}
