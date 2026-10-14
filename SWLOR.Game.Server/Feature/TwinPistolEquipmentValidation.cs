using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature;

/// <summary>Reconciles restored and completed equipment arrangements, including failed equip attempts.</summary>
public static class TwinPistolEquipmentValidation
{
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
        if (!GetIsObjectValid(creature) || (!GetIsPC(creature) && !Droid.IsDroid(creature)) ||
            GetIsDM(creature) || GetIsDMPossessed(creature)) return;

        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        var companion = GetIsObjectValid(off) && GetBaseItemType(off) == BaseItem.CompanionPistol;
        var companionError = companion
            ? Droid.IsDroid(creature) ? "Combat droids cannot equip companion pistols."
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
        if (string.IsNullOrEmpty(mainError) && string.IsNullOrEmpty(companionError)) return;
        // Return the owned item to its existing inventory through the engine; never serialize/copy it.
        AssignCommand(creature, () =>
        {
            ClearAllActions();
            if (!string.IsNullOrEmpty(companionError)) ActionUnequipItem(off);
            if (!string.IsNullOrEmpty(mainError)) ActionUnequipItem(main);
        });
        var observer = Droid.IsDroid(creature) ? GetMaster(creature) : creature;
        SendMessageToPC(observer, ColorToken.Red(!string.IsNullOrEmpty(mainError) ? mainError : companionError));
    }
}
