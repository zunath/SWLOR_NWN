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
    public static string GetUnusableEquipmentError(uint creature)
    {
        if (TwinPistolEquipment.IsPairItem(GetItemInSlot(InventorySlot.RightHand, creature)) &&
            !TwinPistolEquipment.IsActivePair(creature))
            return "Twin Pistols require their generated off-hand pistol. Unequip and re-equip the pair.";
        if (!TwinPistolEquipment.TryGetFailedReturn(creature, out var failedItem, out var failedSlot)) return string.Empty;
        if (!GetIsObjectValid(creature) || GetIsDead(creature) ||
            GetItemInSlot(failedSlot, creature) != failedItem ||
            string.IsNullOrEmpty(Item.CanEquip(creature, failedItem, failedSlot)))
        {
            TwinPistolEquipment.ClearFailedReturn(creature);
            return string.Empty;
        }
        return "Free inventory space or make room in a carried bag, then unequip the unusable weapon.";
    }

    [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
    public static void OnDeath() => TwinPistolEquipment.ClearFailedReturn(OBJECT_SELF);

    // Dismissed droids are destroyed without dying or exiting; forget them before their IDs are reused.
    [NWNEventHandler(ScriptName.OnObjectDestroyed)]
    public static void OnDestroyed() => TwinPistolEquipment.ClearFailedReturn(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit() => TwinPistolEquipment.ClearFailedReturn(GetExitingObject());

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
        if (TwinPistolEquipment.IsRevalidating(creature) || TwinPistolEquipment.IsChanging(creature) ||
            !GetIsObjectValid(creature) || (!GetIsPC(creature) && !Droid.IsDroid(creature)) ||
            GetIsDM(creature) || GetIsDMPossessed(creature)) return;

        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!TryGetHandErrors(creature, out var mainError, out var companionError)) return;
        if (string.IsNullOrEmpty(mainError) && string.IsNullOrEmpty(companionError))
        {
            TwinPistolEquipment.ClearFailedReturn(creature);
            return;
        }
        // Reserve owned storage before native unequipping can drop a crowded player's item.
        AssignCommand(creature, () =>
        {
            if (!TwinPistolEquipment.TryBeginRevalidation(creature)) return;
            try
            {
                ClearAllActions();
                if (!string.IsNullOrEmpty(companionError)) ReturnEquippedItem(creature, off, InventorySlot.LeftHand);
                if (!string.IsNullOrEmpty(mainError)) ReturnEquippedItem(creature, main, InventorySlot.RightHand);
            }
            finally { TwinPistolEquipment.EndRevalidation(creature); }
        });
        var observer = Droid.IsDroid(creature) ? GetMaster(creature) : creature;
        SendMessageToPC(observer, ColorToken.Red(!string.IsNullOrEmpty(mainError) ? mainError : companionError));
    }

    /// <summary>Checks the current hands; false while they are mid-change and must not be judged yet.</summary>
    public static bool TryGetHandErrors(uint creature, out string mainError, out string companionError)
    {
        mainError = companionError = string.Empty;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        // A removed pair's spare stays in the hand until its deferred destroy completes.
        // It is leaving, not misused; its loss event revalidates the settled hands.
        if (TwinPistolEquipment.IsDestroyingCompanion(off)) return false;
        var companion = GetIsObjectValid(off) && GetBaseItemType(off) == BaseItem.CompanionPistol;
        companionError = companion
            ? !TwinPistolEquipment.IsLinkedPair(creature, main, off)
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
        mainError = GetIsObjectValid(main) && Item.PistolBaseItemTypes.Contains(GetBaseItemType(main))
            ? Item.GetCreatureItemUseError(creature, main, skill) : string.Empty;
        return true;
    }

    private static void ReturnEquippedItem(uint creature, uint item, InventorySlot slot)
    {
        if (GetItemInSlot(slot, creature) != item) return;
        // Droids save their controller snapshot on every unacquire, so a dropped weapon would be lost for good.
        if (GetIsPC(creature) || Droid.IsDroid(creature))
        {
            try
            {
                if (!PlayerEquipmentStorage.TryUnequip(creature, item, slot, allowRecoveryBag: false))
                    throw new InvalidOperationException("No owned storage can hold the returned item.");
                if (TwinPistolEquipment.TryGetFailedReturn(creature, out var returnedItem, out _) && returnedItem == item)
                    TwinPistolEquipment.ClearFailedReturn(creature);
            }
            catch (InvalidOperationException) when (GetItemInSlot(slot, creature) == item && GetItemPossessor(item) == creature)
            {
                TwinPistolEquipment.SetFailedReturn(creature, item, slot);
                ClearAllActions();
                SendMessageToPC(Droid.IsDroid(creature) ? GetMaster(creature) : creature, ColorToken.Red(GetUnusableEquipmentError(creature)));
            }
        }
        else ActionUnequipItem(item);
    }
}
