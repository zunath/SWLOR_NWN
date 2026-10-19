using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature;

/// <summary>Reconciles restored and completed equipment arrangements, including failed equip attempts.</summary>
public static class TwinPistolEquipmentValidation
{
    private static readonly HashSet<uint> Revalidating = new();
    private sealed record FailedReturn(uint Item, InventorySlot Slot);
    private static readonly Dictionary<uint, FailedReturn> FailedReturns = new();

    public static string GetUnusableEquipmentError(uint creature)
    {
        if (TwinPistolEquipment.IsPairItem(GetItemInSlot(InventorySlot.RightHand, creature)) &&
            !TwinPistolEquipment.IsActivePair(creature))
            return "Twin Pistols require their generated off-hand pistol. Unequip and re-equip the pair.";
        if (!FailedReturns.TryGetValue(creature, out var failure)) return string.Empty;
        if (!GetIsObjectValid(creature) || GetIsDead(creature) ||
            GetItemInSlot(failure.Slot, creature) != failure.Item ||
            string.IsNullOrEmpty(Item.CanEquip(creature, failure.Item, failure.Slot)))
        {
            FailedReturns.Remove(creature);
            return string.Empty;
        }
        return "Free inventory space or make room in a carried bag, then unequip the unusable weapon.";
    }

    [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
    public static void OnDeath() => FailedReturns.Remove(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit() => FailedReturns.Remove(GetExitingObject());

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter() => QueueRevalidation(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnModuleRespawn)]
    public static void OnRespawn() => QueueRevalidation(GetLastRespawnButtonPresser());

    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnEquipmentChanged() => Revalidate(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire() => QueueRevalidation(GetModuleItemAcquiredBy());

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnUnacquire() => QueueRevalidation(GetModuleItemLostBy());

    private static void QueueRevalidation(uint creature) => DelayCommand(0.1f, () => Revalidate(creature));

    public static void Revalidate(uint creature)
    {
        if (Revalidating.Contains(creature) || TwinPistolEquipment.IsChanging(creature) ||
            !GetIsObjectValid(creature) || (!GetIsPC(creature) && !Droid.IsDroid(creature)) ||
            GetIsDM(creature) || GetIsDMPossessed(creature)) return;

        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        var companion = GetIsObjectValid(off) && GetBaseItemType(off) == BaseItem.CompanionPistol;
        var companionError = companion
            ? Droid.IsDroid(creature) ? "Combat droids cannot equip companion pistols."
                : !TwinPistolEquipment.IsLinkedPair(creature, main, off)
                    ? "The off-hand pistol is created automatically by equipping a Twin Pistols item."
                : TwinPistolEquipment.GetCompanionPropertyError(off)
            : string.Empty;
        if (companion && string.IsNullOrEmpty(companionError))
            companionError = TwinPistolEquipment.GetPairCompatibilityError(
                GetIsObjectValid(main) ? GetBaseItemType(main) : null, BaseItem.CompanionPistol);
        if (companion && string.IsNullOrEmpty(companionError))
            companionError = Item.GetCreatureItemUseError(creature, off);

        var skill = companion && string.IsNullOrEmpty(companionError) && GetIsObjectValid(main) &&
                    TwinPistolEquipment.IsPair(GetBaseItemType(main), GetBaseItemType(off))
            ? SkillType.TwinPistol : SkillType.Invalid;
        var mainError = GetIsObjectValid(main) && Item.PistolBaseItemTypes.Contains(GetBaseItemType(main))
            ? Item.GetCreatureItemUseError(creature, main, skill) : string.Empty;
        if (string.IsNullOrEmpty(mainError) && string.IsNullOrEmpty(companionError))
        {
            FailedReturns.Remove(creature);
            return;
        }
        // Reserve owned storage before native unequipping can drop a crowded player's item.
        AssignCommand(creature, () =>
        {
            if (!Revalidating.Add(creature)) return;
            try
            {
                ClearAllActions();
                if (!string.IsNullOrEmpty(companionError)) ReturnEquippedItem(creature, off, InventorySlot.LeftHand);
                if (!string.IsNullOrEmpty(mainError)) ReturnEquippedItem(creature, main, InventorySlot.RightHand);
            }
            finally { Revalidating.Remove(creature); }
        });
        var observer = Droid.IsDroid(creature) ? GetMaster(creature) : creature;
        SendMessageToPC(observer, ColorToken.Red(!string.IsNullOrEmpty(mainError) ? mainError : companionError));
    }

    private static void ReturnEquippedItem(uint creature, uint item, InventorySlot slot)
    {
        if (GetItemInSlot(slot, creature) != item) return;
        if (GetIsPC(creature))
        {
            try
            {
                if (!PlayerEquipmentStorage.TryUnequip(creature, item, slot, allowRecoveryBag: false))
                    throw new InvalidOperationException("No owned storage can hold the returned item.");
                if (FailedReturns.TryGetValue(creature, out var failure) && failure.Item == item)
                    FailedReturns.Remove(creature);
            }
            catch (InvalidOperationException) when (GetItemInSlot(slot, creature) == item && GetItemPossessor(item) == creature)
            {
                FailedReturns[creature] = new FailedReturn(item, slot);
                ClearAllActions();
                SendMessageToPC(creature, ColorToken.Red(GetUnusableEquipmentError(creature)));
            }
        }
        else ActionUnequipItem(item);
    }
}
