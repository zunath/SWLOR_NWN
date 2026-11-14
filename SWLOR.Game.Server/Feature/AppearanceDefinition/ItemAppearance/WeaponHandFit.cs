using System;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Seats a held pistol in the wearer's hand. Pistols are fitted to the male human default hand; other bodies
/// and a few hand models hold the handle somewhere else, so the pistol is moved by the amount recorded in
/// <see cref="WeaponHandFitCatalog"/>. The move depends only on the hand, never on the pistol or the player.
/// </summary>
public static class WeaponHandFit
{
    private const ObjectVisualTransformDataScopeType Scope = ObjectVisualTransformDataScopeType.Base;
    private const string HeldRightVariable = "WEAPON_HAND_FIT_HELD_RIGHT";
    private const string HeldLeftVariable = "WEAPON_HAND_FIT_HELD_LEFT";

    private static readonly (ObjectVisualTransform Transform, string Variable)[] Axes =
    {
        (ObjectVisualTransform.TranslateX, "WEAPON_HAND_FIT_X"),
        (ObjectVisualTransform.TranslateY, "WEAPON_HAND_FIT_Y"),
        (ObjectVisualTransform.TranslateZ, "WEAPON_HAND_FIT_Z"),
    };

    [NWNEventHandler(ScriptName.OnModuleEquip)]
    public static void OnModuleEquip() => Apply(GetPCItemLastEquippedBy());

    [NWNEventHandler(ScriptName.OnModuleUnequip)]
    public static void OnModuleUnequip() => Release(GetPCItemLastUnequipped());

    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnItemUnequipAfter() => Release(StringToObject(EventsPlugin.GetEventData("ITEM")));

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnModuleUnacquire() => Release(GetModuleItemLost());

    /// <summary>Moves both held pistols to suit the hands the creature shows now.</summary>
    public static void Apply(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        // A replacement need not raise an unequip event for the weapon it displaces.
        ReleaseDisplaced(creature, HeldRightVariable, right, left);
        ReleaseDisplaced(creature, HeldLeftVariable, right, left);
        ApplyHand(creature, right, CreaturePart.RightHand, "r", HeldRightVariable);
        ApplyHand(creature, left, CreaturePart.LeftHand, "l", HeldLeftVariable);
    }

    public static bool IsPistol(BaseItem type) =>
        type is BaseItem.Pistol or BaseItem.Sling or BaseItem.LegacyPistol or BaseItem.CompanionPistol;

    /// <summary>
    /// The transform value that replaces this fit's earlier share with its new one and keeps whatever else
    /// moved the item, such as a transform a DM set.
    /// </summary>
    public static float Rebase(float current, float previous, float next) => current - previous + next;

    private static void ReleaseDisplaced(uint creature, string variable, uint right, uint left)
    {
        var previous = GetLocalObject(creature, variable);
        if (GetIsObjectValid(previous) && previous != right && previous != left)
            SetMove(previous, FitVector.Zero);
    }

    private static void ApplyHand(uint creature, uint item, CreaturePart part, string side, string variable)
    {
        if (!IsFitted(item))
        {
            DeleteLocalObject(creature, variable);
            return;
        }
        SetLocalObject(creature, variable, item);
        SetMove(item, WeaponHandFitCatalog.Get(PowerGloveFit.GetHandModels(creature, part, side).Worn));
    }

    /// <summary>A pistol on the ground or in a pack carries no move.</summary>
    private static void Release(uint item)
    {
        if (!IsFitted(item))
            return;
        // An unequip notice can arrive after the pistol is already back in a hand; that fit must stay.
        var owner = GetItemPossessor(item);
        if (GetIsObjectValid(owner) && GetObjectType(owner) == ObjectType.Creature &&
            (GetItemInSlot(InventorySlot.RightHand, owner) == item || GetItemInSlot(InventorySlot.LeftHand, owner) == item))
            return;
        SetMove(item, FitVector.Zero);
    }

    private static bool IsFitted(uint item) =>
        GetIsObjectValid(item) && GetObjectType(item) == ObjectType.Item && IsPistol(GetBaseItemType(item));

    private static void SetMove(uint item, FitVector move)
    {
        Span<float> values = stackalloc float[] { move.X, move.Y, move.Z };
        for (var index = 0; index < Axes.Length; index++)
        {
            var (transform, variable) = Axes[index];
            var current = GetObjectVisualTransform(item, transform, nScope: Scope);
            var next = Rebase(current, GetLocalFloat(item, variable), values[index]);
            if (Math.Abs(current - next) > .000001f)
                SetObjectVisualTransform(item, transform, next, nScope: Scope);
            // The item saves both the transform and this share of it, so a reload stays consistent.
            if (values[index] == 0f) DeleteLocalFloat(item, variable);
            else SetLocalFloat(item, variable, values[index]);
        }
    }
}
