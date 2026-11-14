using System;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;
using Turn = System.Numerics.Quaternion;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Seats a held pistol or rifle in the wearer's hand. Both are fitted to the male human body; other bodies and
/// hand models hold the handle somewhere else, so the weapon is moved by the amount recorded in
/// <see cref="WeaponHandFitCatalog"/>. The move depends only on the hand, never on the weapon or the player.
/// </summary>
public static class WeaponHandFit
{
    private const ObjectVisualTransformDataScopeType Scope = ObjectVisualTransformDataScopeType.Base;
    private const string HeldRightVariable = "WEAPON_HAND_FIT_HELD_RIGHT";
    private const string HeldLeftVariable = "WEAPON_HAND_FIT_HELD_LEFT";
    private const string FittedItemVariable = "WEAPON_HAND_FIT_ITEM";

    // The rifle ready and attack clips turn the weapon hook against the hand by a fixed amount; the idle
    // clip leaves it at rest. The female dwarf and half-orc frames inherit clips that turn it less.
    // Measured from the compiled clips (hook orientation in the hand bone, as x y z w).
    private static readonly Turn RifleReadyTurn = new(.4155f, -.1652f, .0770f, .8911f);
    private static readonly Turn RifleReadyTurnShortFrames = new(.3120f, -.1728f, .0578f, .9324f);

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

    /// <summary>Moves the held pistols or rifle to suit the hands the creature shows now.</summary>
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

    public static bool IsRifle(BaseItem type) => type is BaseItem.Rifle or BaseItem.Cannon;

    /// <summary>
    /// The move for a rifle held in a hand whose pistol move is <paramref name="move"/>. A held item takes one
    /// fixed move, but the ready and attack clips turn the rifle against the hand, so the same hand needs a
    /// different move there than in the idle clip. This is the midpoint: the part of the move along the turn's
    /// axis is exact in every clip and the rest is half off in each, instead of right in one and wrong in the other.
    /// </summary>
    public static FitVector RifleMove(FitVector move, string handModel)
    {
        if (move == FitVector.Zero)
            return move;
        var shortFrame = handModel != null &&
                         (handModel.StartsWith("pfd0", StringComparison.OrdinalIgnoreCase) ||
                          handModel.StartsWith("pfo0", StringComparison.OrdinalIgnoreCase));
        var ready = FitVector.Transform(move, Turn.Conjugate(shortFrame ? RifleReadyTurnShortFrames : RifleReadyTurn));
        return (move + ready) / 2f;
    }

    /// <summary>
    /// The transform value that replaces this fit's earlier share with its new one and keeps whatever else
    /// moved the item, such as a transform a DM set.
    /// </summary>
    public static float Rebase(float current, float previous, float next) => current - previous + next;

    /// <summary>
    /// The share of an item's transform this fit set earlier. A copied item inherits the original's recorded
    /// share but not its transform, so a share recorded for another item counts as none.
    /// </summary>
    public static float OwnedShare(string fittedItem, string currentItem, float share) =>
        !string.IsNullOrEmpty(fittedItem) && fittedItem == currentItem ? share : 0f;

    private static void ReleaseDisplaced(uint creature, string variable, uint right, uint left)
    {
        var previous = GetLocalObject(creature, variable);
        if (!GetIsObjectValid(previous) || previous == right || previous == left)
            return;
        // The weapon may already be in someone else's hand with their fit; Release leaves that alone.
        DeleteLocalObject(creature, variable);
        Release(previous);
    }

    private static void ApplyHand(uint creature, uint item, CreaturePart part, string side, string variable)
    {
        if (!IsFitted(item))
        {
            DeleteLocalObject(creature, variable);
            return;
        }
        SetLocalObject(creature, variable, item);
        var hand = PowerGloveFit.GetHandModels(creature, part, side).Worn;
        var move = WeaponHandFitCatalog.Get(hand);
        SetMove(item, IsRifle(GetBaseItemType(item)) ? RifleMove(move, hand) : move);
    }

    /// <summary>A weapon on the ground or in a pack carries no move.</summary>
    private static void Release(uint item)
    {
        if (!IsFitted(item))
            return;
        // An unequip notice can arrive after the weapon is already back in a hand; that fit must stay.
        var owner = GetItemPossessor(item);
        if (GetIsObjectValid(owner) && GetObjectType(owner) == ObjectType.Creature &&
            (GetItemInSlot(InventorySlot.RightHand, owner) == item || GetItemInSlot(InventorySlot.LeftHand, owner) == item))
            return;
        SetMove(item, FitVector.Zero);
    }

    private static bool IsFitted(uint item)
    {
        if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
            return false;
        var type = GetBaseItemType(item);
        return IsPistol(type) || IsRifle(type);
    }

    private static void SetMove(uint item, FitVector move)
    {
        var fittedItem = GetLocalString(item, FittedItemVariable);
        if (move == FitVector.Zero && string.IsNullOrEmpty(fittedItem))
            return;
        // CopyItem copies locals but resets visual transforms; UUIDs survive ordinary reloads.
        var identity = GetObjectUUID(item);
        Span<float> values = stackalloc float[] { move.X, move.Y, move.Z };
        for (var index = 0; index < Axes.Length; index++)
        {
            var (transform, variable) = Axes[index];
            var current = GetObjectVisualTransform(item, transform, nScope: Scope);
            var previous = OwnedShare(fittedItem, identity, GetLocalFloat(item, variable));
            var next = Rebase(current, previous, values[index]);
            if (Math.Abs(current - next) > .000001f)
                SetObjectVisualTransform(item, transform, next, nScope: Scope);
            // The item saves both the transform and this share of it, so a reload stays consistent.
            if (values[index] == 0f) DeleteLocalFloat(item, variable);
            else SetLocalFloat(item, variable, values[index]);
        }
        if (move == FitVector.Zero) DeleteLocalString(item, FittedItemVariable);
        else SetLocalString(item, FittedItemVariable, identity);
    }
}
