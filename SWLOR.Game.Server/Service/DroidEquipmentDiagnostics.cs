using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// The static API of the droid equipment diagnostics. The logic and state live in
/// <see cref="IDroidEquipmentDiagnosticsService"/>; every member here forwards to the container's instance, and the
/// event handlers stay here so their names in the handler order manifest do not change.
/// </summary>
public static class DroidEquipmentDiagnostics
{
    private static IDroidEquipmentDiagnosticsService Instance => FacadeAccessor.Resolve<IDroidEquipmentDiagnosticsService>();

    /// <summary>The most recent entry written, for engine tests.</summary>
    public static string LastEntry => Instance.LastEntry;

    // Native equips (RunEquip): equip actions, inventory drags and queued combat-round equips.
    // The after-event also follows refused equips; only an item now in a slot was equipped.
    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    public static void OnEquipped()
    {
        Instance.OnEquipped();
    }

    // The after-event runs once the item has left its slot, so note the slot first.
    [NWNEventHandler(ScriptName.OnEquipmentUnequipBefore)]
    public static void OnUnequipping()
    {
        Instance.OnUnequipping();
    }

    // Native unequips (RunUnequip): unequip actions, inventory drags and queued combat-round unequips.
    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnUnequipped()
    {
        Instance.OnUnequipped();
    }

    /// <summary>
    /// Engine paths such as disarm, polymorph, replacement equips and item level checks change hands
    /// without either event. The heartbeat compares the hands with the last state seen.
    /// </summary>
    [NWNEventHandler(ScriptName.OnDroidHeartbeat)]
    public static void CheckHands()
    {
        Instance.CheckHands();
    }

    /// <summary>Sets the hand baseline once a summon has restored the droid's equipment, without logging the restore.</summary>
    public static void RememberHands(uint droid)
    {
        Instance.RememberHands(droid);
    }

    [NWNEventHandler(ScriptName.OnObjectDestroyed)]
    public static void Forget()
    {
        Instance.Forget();
    }

    public static string Format(IEnumerable<(string Name, string Value)> fields)
    {
        return Instance.Format(fields);
    }

    /// <summary>
    /// Reduces a managed stack trace to the SWLOR methods that led to the change. An empty result means
    /// no SWLOR code made the change directly: an engine action, the action queue or a client message did.
    /// </summary>
    public static string SummarizeStack(string stackTrace)
    {
        return Instance.SummarizeStack(stackTrace);
    }
}
