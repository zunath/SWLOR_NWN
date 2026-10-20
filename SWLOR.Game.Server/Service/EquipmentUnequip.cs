using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service;

/// <summary>Lets equipment owners reserve safe removal before any shared equipment hooks run.</summary>
public static class EquipmentUnequip
{
    private static readonly Dictionary<string, Func<uint, uint, bool>> Guards = new(StringComparer.Ordinal);

    public static void RegisterGuard(string key, Func<uint, uint, bool> guard)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(guard);
        Guards[key] = guard;
    }

    public static bool TryHandle(uint creature, uint item)
    {
        foreach (var guard in Guards.Values)
            if (guard(creature, item)) return true;
        return false;
    }

    [NWNEventHandler(ScriptName.OnEquipmentUnequipBefore)]
    public static void BeforeUnequip()
    {
        var creature = OBJECT_SELF;
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        if (TryHandle(creature, item))
        {
            EventsPlugin.SkipEvent();
            return;
        }
        ExecuteScript(ScriptName.OnItemUnequipBefore, creature);
    }
}
