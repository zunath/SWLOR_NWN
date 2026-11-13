using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature;

/// <summary>Suppresses the native carrying overlays for two-handed melee weapons.</summary>
public static class MeleeWeaponCarry
{
    private const string Active = "_MELEE_LOWERED_CARRY";
    private static readonly string[] Sources = { "plpause1", "walk_swordr", "run_swordr" };

    public static bool AppliesTo(uint creature) => GetIsObjectValid(creature) &&
        UsesLoweredCarry(AbilityAnimationEquipment.ReadGrip(
            GetItemInSlot(InventorySlot.RightHand, creature), (int)GetCreatureSize(creature)));

    public static bool UsesLoweredCarry(AnimationWeaponGrip grip) =>
        grip is AnimationWeaponGrip.TwoHanded or AnimationWeaponGrip.Polearm;

    public static string RestoreMapping(uint creature, string source, string replacement) =>
        source == "plpause1" && string.IsNullOrEmpty(replacement) && AppliesTo(creature)
            ? NamedAnimationPlayback.NoHoldName : replacement;

    [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
    public static void OnEquipBefore()
    {
        if ((InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT")) != InventorySlot.RightHand) return;
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        Apply(OBJECT_SELF, UsesLoweredCarry(AbilityAnimationEquipment.ReadGrip(item, (int)GetCreatureSize(OBJECT_SELF))));
    }

    [NWNEventHandler(ScriptName.OnModuleEquip)]
    public static void OnEquip() => Synchronize(GetPCItemLastEquippedBy());

    [NWNEventHandler(ScriptName.OnModuleUnequip)]
    public static void OnUnequip() => Synchronize(GetPCItemLastUnequippedBy());

    public static void Synchronize(uint creature)
    {
        if (GetIsObjectValid(creature)) Apply(creature, AppliesTo(creature));
    }

    private static void Apply(uint creature, bool enabled)
    {
        if (!enabled && !GetLocalBool(creature, Active)) return;
        foreach (var source in Sources)
            ReplaceObjectAnimation(creature, source, enabled ? NamedAnimationPlayback.NoHoldName : "");
        SetLocalBool(creature, Active, enabled);
    }
}
