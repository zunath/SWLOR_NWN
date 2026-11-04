using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Applies a player's small position, rotation, and scale correction to each held weapon. The fit
/// is stored on the owned item per hand, so a generated second Twin Pistol uses its owner's
/// off-hand fit. Power Gloves are excluded because <see cref="PowerGloveFit"/> owns their transforms.
/// </summary>
public static class WeaponFit
{
    private const ObjectVisualTransformDataScopeType Scope = ObjectVisualTransformDataScopeType.Base;

    private const string FittedRightVariable = "WEAPON_FIT_HELD_RIGHT";
    private const string FittedLeftVariable = "WEAPON_FIT_HELD_LEFT";

    [NWNEventHandler(ScriptName.OnModuleEquip)]
    public static void OnModuleEquip() => Apply(GetPCItemLastEquippedBy());

    [NWNEventHandler(ScriptName.OnModuleUnequip)]
    public static void OnModuleUnequip() => Release(GetPCItemLastUnequipped());

    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnItemUnequipAfter() => Release(StringToObject(EventsPlugin.GetEventData("ITEM")));

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnModuleUnacquire() => Release(GetModuleItemLost());

    /// <summary>True when the item in this hand may be fitted by its owner.</summary>
    public static bool CanFit(uint creature, InventorySlot hand) =>
        IsFittable(GetItemInSlot(hand, creature));

    public static int GetSteps(uint owner, InventorySlot hand, WeaponFitAxis axis) =>
        WeaponFitLimits.ClampSteps(axis, GetLocalInt(owner, GetVariableName(hand, axis)));

    /// <summary>Stores a clamped fit on the owned item and refreshes the creature's held weapons.</summary>
    public static void SetSteps(uint creature, uint owner, InventorySlot hand, WeaponFitAxis axis, int steps)
    {
        steps = WeaponFitLimits.ClampSteps(axis, steps);
        var variable = GetVariableName(hand, axis);
        if (steps == 0)
            DeleteLocalInt(owner, variable);
        else
            SetLocalInt(owner, variable, steps);
        Apply(creature);
    }

    public static void Reset(uint creature, uint owner, InventorySlot hand)
    {
        foreach (var axis in Enum.GetValues<WeaponFitAxis>())
            DeleteLocalInt(owner, GetVariableName(hand, axis));
        Apply(creature);
    }

    /// <summary>Applies the stored fit to both held weapons.</summary>
    public static void Apply(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        // A replacement need not raise a separate unequip event for the displaced weapon.
        ReleaseDisplaced(creature, FittedRightVariable, right, left);
        ReleaseDisplaced(creature, FittedLeftVariable, right, left);
        ApplyHand(creature, InventorySlot.RightHand, right, FittedRightVariable);
        ApplyHand(creature, InventorySlot.LeftHand, left, FittedLeftVariable);
    }

    private static void ReleaseDisplaced(uint creature, string variable, uint right, uint left)
    {
        var previous = GetLocalObject(creature, variable);
        if (GetIsObjectValid(previous) && previous != right && previous != left)
            Release(previous);
    }

    private static void ApplyHand(uint creature, InventorySlot hand, uint item, string variable)
    {
        if (!IsFittable(item))
        {
            DeleteLocalObject(creature, variable);
            return;
        }
        SetLocalObject(creature, variable, item);
        var owner = TwinPistolEquipment.ResolveAppearanceOwner(creature, item);
        if (!GetIsObjectValid(owner))
            owner = item;
        foreach (var axis in Enum.GetValues<WeaponFitAxis>())
            SetTransform(item, axis, WeaponFitLimits.GetValue(axis, GetSteps(owner, hand, axis)));
    }

    /// <summary>A weapon on the ground or in a pack carries no fit.</summary>
    private static void Release(uint item)
    {
        if (!IsFittable(item))
            return;
        foreach (var axis in Enum.GetValues<WeaponFitAxis>())
            SetTransform(item, axis, WeaponFitLimits.GetValue(axis, 0));
    }

    private static bool IsFittable(uint item) =>
        GetIsObjectValid(item) &&
        GetObjectType(item) == ObjectType.Item &&
        GetBaseItemType(item) != BaseItem.PowerGlove &&
        !PowerGloveEquipment.IsGeneratedCompanion(item);

    private static void SetTransform(uint item, WeaponFitAxis axis, float value)
    {
        var transform = GetTransform(axis);
        if (Math.Abs(GetObjectVisualTransform(item, transform, nScope: Scope) - value) > .00001f)
            SetObjectVisualTransform(item, transform, value, nScope: Scope);
    }

    private static ObjectVisualTransform GetTransform(WeaponFitAxis axis) => axis switch
    {
        WeaponFitAxis.TranslateX => ObjectVisualTransform.TranslateX,
        WeaponFitAxis.TranslateY => ObjectVisualTransform.TranslateY,
        WeaponFitAxis.TranslateZ => ObjectVisualTransform.TranslateZ,
        WeaponFitAxis.RotateX => ObjectVisualTransform.RotateX,
        WeaponFitAxis.RotateY => ObjectVisualTransform.RotateY,
        WeaponFitAxis.RotateZ => ObjectVisualTransform.RotateZ,
        WeaponFitAxis.Scale => ObjectVisualTransform.Scale,
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };

    public static string GetVariableName(InventorySlot hand, WeaponFitAxis axis) =>
        $"WEAPON_FIT_{(hand == InventorySlot.LeftHand ? "LEFT" : "RIGHT")}_{axis}";
}
