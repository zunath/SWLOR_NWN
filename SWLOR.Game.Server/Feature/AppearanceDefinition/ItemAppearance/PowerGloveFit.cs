using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>Fits the compact Power Glove shells without changing their authored appearance.</summary>
public static class PowerGloveFit
{
    private const ObjectVisualTransformDataScopeType FitScope =
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2;
    private const string AppliedTranslationXVariable = "POWER_GLOVE_FIT_X";
    private const string AppliedTranslationYVariable = "POWER_GLOVE_FIT_Y";
    private const string AppliedTranslationZVariable = "POWER_GLOVE_FIT_Z";

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
        var owner = GetItemPossessor(item);
        NormalizeCurrentItem(item);
        if (GetObjectType(owner) == ObjectType.Creature)
            Apply(owner);
    }

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnUnacquire()
    {
        // A delayed relocation callback must not strip a fit already applied after re-equipping.
        NormalizeCurrentItem(GetModuleItemLost());
    }

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

    private static void NormalizeCurrentItem(uint item)
    {
        var owner = GetItemPossessor(item);
        var creature = GetObjectType(owner) == ObjectType.Creature;
        NormalizeItemAndContents(item,
            creature ? GetItemInSlot(InventorySlot.RightHand, owner) : OBJECT_INVALID,
            creature ? GetItemInSlot(InventorySlot.LeftHand, owner) : OBJECT_INVALID);
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

        // The off-hand carrier follows the owned item's cosmetics in place, preserving combat setup.
        PowerGloveEquipment.RefreshAppearance(creature);

        var rightModel = GetHandModel(creature, CreaturePart.RightHand, "r");
        var leftModel = GetHandModel(creature, CreaturePart.LeftHand, "l");
        Fit(right, rightModel, rightModel, leftModel);
        Fit(left, leftModel, rightModel, leftModel);
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
        var phenotype = RobeModelRenderer.GetBasePhenotype(creature);
        var model = ResolveHandModel(Get2DAString("appearance", "RACE", appearance), GetGender(creature),
            phenotype, side, GetCreatureBodyPart(part, creature), armorPart);
        return ResolveAvailableHandModel(model, phenotype, ModelResource.Exists);
    }

    private static string ResolveHandModel(string race, Gender gender, int phenotype, string side, int naturalPart, int armorPart)
    {
        // Native bare-hand armor styles 0, 1 and 2 display the wearer's natural hand.
        var part = armorPart > 2 ? armorPart : naturalPart;
        if (string.IsNullOrWhiteSpace(race) || race.Length != 1 || part <= 0)
            return string.Empty;
        return $"p{(gender == Gender.Female ? 'f' : 'm')}{race.ToLowerInvariant()}{phenotype}_hand{side}{part:D3}";
    }

    private static string ResolveAvailableHandModel(string model, int phenotype, Func<string, bool> modelExists)
    {
        if (phenotype != 2 || string.IsNullOrEmpty(model) || model.Length < 5 || model[3] != '2' ||
            modelExists(model))
            return model;

        // Native wide bodies use the normal hand resource when their corresponding part is absent.
        var normalModel = model[..3] + "0" + model[4..];
        return modelExists(normalModel) ? normalModel : model;
    }

    private static void Fit(uint item, string handModel, string rightModel, string leftModel)
    {
        if (!IsPowerGlove(item))
            return;
        var appearance = GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle) * 100 +
                         GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle);
        var scale = ResolveScale(rightModel, leftModel, appearance);
        SetScale(item, scale);
        ApplyTranslation(item, CanFitPair(rightModel, leftModel, appearance)
            ? PowerGloveGripFits.GetTranslation(handModel, appearance, scale)
            : FitVector.Zero);
    }

    private static float ResolveScale(string rightModel, string leftModel, int appearance) =>
        CanFitPair(rightModel, leftModel, appearance)
            ? Math.Max(PowerGloveHandFitCatalog.GetScale(rightModel, appearance),
                PowerGloveHandFitCatalog.GetScale(leftModel, appearance))
            : 1f;

    private static bool CanFitPair(string rightModel, string leftModel, int appearance)
    {
        var requiresMeasuredPair = PowerGloveGripFits.IsCorrected(appearance) ||
                                   PowerGloveHandFitOverrides.HasFit(rightModel, appearance) ||
                                   PowerGloveHandFitOverrides.HasFit(leftModel, appearance);
        return !requiresMeasuredPair || PowerGloveHandFitCatalog.HasMeasuredFit(rightModel, appearance) &&
               PowerGloveHandFitCatalog.HasMeasuredFit(leftModel, appearance);
    }

    private static bool IsPowerGlove(uint item) => GetIsObjectValid(item) &&
        GetObjectType(item) == ObjectType.Item && (GetBaseItemType(item) == BaseItem.PowerGlove ||
            PowerGloveEquipment.IsGeneratedCompanion(item) && PowerGloveEquipment.IsLinkedPair(
                GetItemPossessor(item), GetLocalObject(item, PowerGloveEquipment.MainWeaponVariable), item));

    private static void Release(uint item)
    {
        if (IsPowerGlove(item))
        {
            ApplyTranslation(item, FitVector.Zero);
            SetScale(item, 1f);
        }
    }

    /// <summary>Mirrors authored transforms while retaining each hand's separately owned fit offset.</summary>
    internal static float GetMirroredTransform(uint main, uint companion, ObjectVisualTransform transform,
        ObjectVisualTransformDataScopeType scope)
    {
        var value = GetObjectVisualTransform(main, transform, nScope: scope);
        var variable = scope == FitScope ? transform switch
        {
            ObjectVisualTransform.TranslateX => AppliedTranslationXVariable,
            ObjectVisualTransform.TranslateY => AppliedTranslationYVariable,
            ObjectVisualTransform.TranslateZ => AppliedTranslationZVariable,
            _ => null
        } : null;
        return variable == null ? value : value - GetLocalFloat(main, variable) + GetLocalFloat(companion, variable);
    }

    private static FitVector RebaseTranslation(FitVector current, FitVector previousOffset, FitVector nextOffset) =>
        current - previousOffset + nextOffset;

    private static void ApplyTranslation(uint item, FitVector offset)
    {
        var current = new FitVector(
            GetObjectVisualTransform(item, ObjectVisualTransform.TranslateX, nScope: FitScope),
            GetObjectVisualTransform(item, ObjectVisualTransform.TranslateY, nScope: FitScope),
            GetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ, nScope: FitScope));
        var previous = new FitVector(GetLocalFloat(item, AppliedTranslationXVariable),
            GetLocalFloat(item, AppliedTranslationYVariable), GetLocalFloat(item, AppliedTranslationZVariable));
        var next = RebaseTranslation(current, previous, offset);
        SetTranslationAxis(item, ObjectVisualTransform.TranslateX, next.X, AppliedTranslationXVariable, offset.X);
        SetTranslationAxis(item, ObjectVisualTransform.TranslateY, next.Y, AppliedTranslationYVariable, offset.Y);
        SetTranslationAxis(item, ObjectVisualTransform.TranslateZ, next.Z, AppliedTranslationZVariable, offset.Z);
    }

    private static void SetTranslationAxis(uint item, ObjectVisualTransform transform, float value,
        string variable, float appliedOffset)
    {
        if (Math.Abs(GetObjectVisualTransform(item, transform, nScope: FitScope) - value) > .000001f)
            SetObjectVisualTransform(item, transform, value, nScope: FitScope);
        // Native serialization retains both this delta and the fitted transform.
        if (appliedOffset == 0f) DeleteLocalFloat(item, variable);
        else SetLocalFloat(item, variable, appliedOffset);
    }

    private static void SetScale(uint item, float scale)
    {
        // Absolute, part-scoped values cannot compound after export/import or repeated refreshes.
        // Base transforms and every rotation remain independently owned.
        if (Math.Abs(GetObjectVisualTransform(item, ObjectVisualTransform.Scale, nScope: FitScope) - scale) > 0.0001f)
            SetObjectVisualTransform(item, ObjectVisualTransform.Scale, scale, nScope: FitScope);
    }
}
