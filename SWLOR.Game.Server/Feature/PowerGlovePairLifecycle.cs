using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature;

/// <summary>Routes both-hand removal through safe storage before the existing equipment hooks run.</summary>
public static class PowerGlovePairLifecycle
{
    [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
    public static void RegisterUnequipGuard() => EquipmentUnequip.RegisterGuard("PowerGlove", TryHandleUnequip);

    [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
    public static void RegisterAttackRules()
    {
        WeaponAttackEligibility.RegisterRule(BaseItem.PowerGlove, (actor, weapon, _) =>
            GetItemInSlot(InventorySlot.RightHand, actor) == weapon && PowerGloveEquipment.IsActivePair(actor));
        WeaponAttackEligibility.RegisterRule(BaseItem.CompanionPowerGlove, (_, _, _) => false);
    }

    public static bool TryHandleUnequip(uint actor, uint item)
    {
        if (!PowerGloveEquipment.IsChanging(actor) && !PowerGloveEquipment.IsDestroyingCompanion(item) &&
            (PowerGloveEquipment.IsPairItem(item) || PowerGloveEquipment.IsGeneratedCompanion(item)))
        {
            // Only the nested, storage-reserved real unequip dispatches stat/perk removal.
            PowerGloveEquipment.RemovePair(actor, item);
            return true;
        }
        return false;
    }

    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    public static void AfterEquip() => PowerGloveEquipment.EnsurePair(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void AfterUnequip()
    {
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        if (PowerGloveEquipment.IsDestroyingCompanion(item)) return;
        if (PowerGloveEquipment.IsPairItem(item) || PowerGloveEquipment.IsGeneratedCompanion(item))
            PowerGloveEquipment.RemovePair(OBJECT_SELF, item);
    }

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter() => PowerGloveEquipment.RestorePair(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnPlayerSpawn)]
    public static void OnSpawn() => PowerGloveEquipment.RestorePair(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit() => PowerGloveEquipment.ReleaseCompanions(GetExitingObject());

    [NWNEventHandler(ScriptName.OnObjectDestroyed)]
    public static void OnDestroyed() => PowerGloveEquipment.ReleaseDestroyedOwner(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleDeath)]
    public static void OnDeath() => PowerGloveEquipment.EnsurePair(GetLastPlayerDied());

    [NWNEventHandler(ScriptName.OnModuleRespawn)]
    public static void OnRespawn() => PowerGloveEquipment.EnsurePair(GetLastRespawnButtonPresser());

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnLostItem()
    {
        var item = GetModuleItemLost();
        if (PowerGloveEquipment.IsDestroyingCompanion(item)) return;
        var companions = new HashSet<uint>();
        PowerGloveEquipment.CollectCompanions(item, companions, new HashSet<uint>());
        foreach (var companion in companions)
        {
            var owner = GetLocalObject(companion, PowerGloveEquipment.OwnerVariable);
            // Module callbacks can arrive after storage relocation and native equip finish.
            // Current linked ownership distinguishes that completed move from a real escape.
            if (PowerGloveEquipment.IsChanging(owner) || PowerGloveEquipment.IsEquippedCompanion(companion))
                continue;
            PowerGloveEquipment.RemovePair(owner, companion);
            PowerGloveEquipment.DestroyCompanion(companion);
        }
        var actor = GetModuleItemLostBy();
        if (PowerGloveEquipment.IsPairItem(item) && !PowerGloveEquipment.IsChanging(actor) &&
            !(GetItemPossessor(item) == actor && GetItemInSlot(InventorySlot.RightHand, actor) == item &&
              PowerGloveEquipment.IsEquippedCompanion(GetItemInSlot(InventorySlot.LeftHand, actor))))
            PowerGloveEquipment.RemovePair(actor, item);
    }

    [NWNEventHandler(ScriptName.OnItemAcquireBefore)]
    public static void BeforeAcquire()
    {
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        if (!PowerGloveEquipment.IsGeneratedCompanion(item)) return;
        if (PowerGloveEquipment.IsCreatingCompanion) return;
        var owner = GetLocalObject(item, PowerGloveEquipment.OwnerVariable);
        if (PowerGloveEquipment.IsChanging(owner) || PowerGloveEquipment.IsDestroyingCompanion(item)) return;
        EventsPlugin.SkipEvent();
        if (!GetIsObjectValid(owner) || PowerGloveEquipment.RemovePair(owner, item))
            PowerGloveEquipment.DestroyCompanion(item);
    }

    /// <summary>Gloves made before the per-hand note, including ones players already own, gain it when examined.</summary>
    [NWNEventHandler(ScriptName.OnExamineObjectBefore)]
    public static void OnExamine()
    {
        var item = StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID"));
        if (!PowerGloveEquipment.IsPairItem(item)) return;
        var description = GetDescription(item);
        var noted = PowerGloveEquipment.GetPairDescription(description);
        if (noted != description) SetDescription(item, noted);
    }

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var companions = new HashSet<uint>();
        PowerGloveEquipment.CollectCompanions(GetModuleItemAcquired(), companions, new HashSet<uint>());
        foreach (var companion in companions)
        {
            if (PowerGloveEquipment.IsDestroyingCompanion(companion)) continue;
            if (PowerGloveEquipment.IsCreatingCompanion)
            {
                PowerGloveEquipment.SanitizeCompanion(companion);
                continue;
            }
            var linkedOwner = GetLocalObject(companion, PowerGloveEquipment.OwnerVariable);
            if (PowerGloveEquipment.IsEquippedCompanion(companion) ||
                PowerGloveEquipment.TryRestoreSavedCompanion(companion) ||
                PowerGloveEquipment.IsChanging(linkedOwner) && GetItemPossessor(companion) == linkedOwner)
                PowerGloveEquipment.SanitizeCompanion(companion);
            else
            {
                PowerGloveEquipment.RemovePair(linkedOwner, companion);
                PowerGloveEquipment.DestroyCompanion(companion);
            }
        }
    }
}
