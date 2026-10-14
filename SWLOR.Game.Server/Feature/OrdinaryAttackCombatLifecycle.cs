using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature;

public static class OrdinaryAttackCombatLifecycle
{
    [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
    public static void Reconcile()
    {
        var actor = OBJECT_SELF;
        if (GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0 ||
            !GetIsInCombat(actor) && !Activity.IsBusy(actor))
            OrdinaryAttackTraits.Clear(actor);
        else OrdinaryAttackTraits.Reconcile(actor);
    }

    [NWNEventHandler(ScriptName.OnAreaExit)]
    public static void OnAreaExit() => OrdinaryAttackTraits.Clear(GetExitingObject());
    [NWNEventHandler(ScriptName.OnModuleDeath)]
    public static void OnDeath() => OrdinaryAttackTraits.Clear(GetLastPlayerDied());
    [NWNEventHandler(ScriptName.OnModuleDying)]
    public static void OnDying() => OrdinaryAttackTraits.Clear(GetLastPlayerDying());
    [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
    public static void OnCreatureDeath() => OrdinaryAttackTraits.Clear(OBJECT_SELF);
    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit() => OrdinaryAttackTraits.ClearActor(GetExitingObject());
}
