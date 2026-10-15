using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature;

public static class OrdinaryAttackCombatLifecycle
{
    [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
    public static void Reconcile()
    {
        var actor = OBJECT_SELF;
        if (GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0)
            OrdinaryAttackTraits.ClearOnDeath(actor);
        else if (!GetIsInCombat(actor) && !Activity.IsBusy(actor))
            OrdinaryAttackTraits.Clear(actor);
        else OrdinaryAttackTraits.Reconcile(actor);
    }

    [NWNEventHandler(ScriptName.OnAreaExit)]
    public static void OnAreaExit() => OrdinaryAttackTraits.Clear(GetExitingObject());
    [NWNEventHandler(ScriptName.OnModuleDeath)]
    public static void OnDeath() => OrdinaryAttackTraits.ClearOnDeath(GetLastPlayerDied());
    [NWNEventHandler(ScriptName.OnModuleDying)]
    public static void OnDying()
    {
        var actor = GetLastPlayerDying();
        // Fatal-damage protection can restore the player before dying handlers finish.
        DelayCommand(0f, () =>
        {
            if (GetIsObjectValid(actor) && (GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0))
                OrdinaryAttackTraits.ClearOnDeath(actor);
        });
    }
    [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
    public static void OnCreatureDeath() => OrdinaryAttackTraits.ClearOnDeath(OBJECT_SELF);
    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void OnExit() => OrdinaryAttackTraits.ClearActor(GetExitingObject());
}
