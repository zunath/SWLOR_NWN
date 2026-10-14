using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature;

/// <summary>The second gun exists only while its owned Twin Pistols item is equipped.</summary>
public static class TwinPistolPairLifecycle
{
    [NWNEventHandler(ScriptName.OnTwinPistolUnequipBefore)]
    public static void BeforeUnequip()
    {
        var actor = OBJECT_SELF;
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        if (!TwinPistolEquipment.IsChanging(actor) && !TwinPistolEquipment.IsDestroyingCompanion(item) &&
            (TwinPistolEquipment.IsPairItem(item) || TwinPistolEquipment.IsGeneratedCompanion(item)))
        {
            // Nested safe removal dispatches the existing stat/perk hooks exactly once.
            // The outer request must never drop the real item or run those hooks a second time.
            TwinPistolEquipment.RemovePair(actor, item);
            EventsPlugin.SkipEvent();
            return;
        }
        ExecuteScript(ScriptName.OnItemUnequipBefore, actor);
    }

    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    public static void AfterEquip() => TwinPistolEquipment.EnsurePair(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void AfterUnequip()
    {
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        if (TwinPistolEquipment.IsDestroyingCompanion(item)) return;
        if (TwinPistolEquipment.IsPairItem(item) || TwinPistolEquipment.IsGeneratedCompanion(item))
            TwinPistolEquipment.RemovePair(OBJECT_SELF, item);
    }

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter() => TwinPistolEquipment.RestorePair(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit() => TwinPistolEquipment.ReleaseCompanions(GetExitingObject());

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnLostItem()
    {
        var item = GetModuleItemLost();
        if (TwinPistolEquipment.IsDestroyingCompanion(item)) return;
        var actor = TwinPistolEquipment.IsGeneratedCompanion(item)
            ? GetLocalObject(item, TwinPistolEquipment.OwnerVariable) : GetModuleItemLostBy();
        if (!TwinPistolEquipment.IsChanging(actor) &&
            (TwinPistolEquipment.IsPairItem(item) || TwinPistolEquipment.IsGeneratedCompanion(item)))
        {
            TwinPistolEquipment.RemovePair(actor, item);
            TwinPistolEquipment.DestroySpare(item);
        }
    }

    [NWNEventHandler(ScriptName.OnItemAcquireBefore)]
    public static void BeforeAcquire()
    {
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        if (!TwinPistolEquipment.IsGeneratedCompanion(item)) return;
        var owner = GetLocalObject(item, TwinPistolEquipment.OwnerVariable);
        if (!GetIsObjectValid(owner) || TwinPistolEquipment.IsChanging(owner)) return;
        EventsPlugin.SkipEvent();
        if (TwinPistolEquipment.RemovePair(owner, item)) TwinPistolEquipment.DestroySpare(item);
    }
}
