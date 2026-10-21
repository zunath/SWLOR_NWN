using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Logs equipment changes on player droids outside their summon restore, so reports of droids
/// dropping their weapons mid-fight can be traced to the code path that moved the item.
/// </summary>
public static class DroidEquipmentDiagnostics
{
    private const int MaxStackFrames = 8;
    private static readonly Dictionary<uint, string> _lastHands = new();
    private static readonly Dictionary<uint, (uint Item, InventorySlot Slot)> _unequipSlots = new();

    /// <summary>The most recent entry written, for engine tests.</summary>
    public static string LastEntry { get; private set; } = string.Empty;

    // Native equips (RunEquip): equip actions, inventory drags and queued combat-round equips.
    // The after-event also follows refused equips; only an item now in a slot was equipped.
    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    public static void OnEquipped()
    {
        var droid = OBJECT_SELF;
        if (!Droid.IsDroid(droid))
            return;

        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        var slot = Item.GetItemSlot(droid, item);
        if (slot != InventorySlot.Invalid)
            Record("equipped", droid, item, slot);
    }

    // The after-event runs once the item has left its slot, so note the slot first.
    [NWNEventHandler(ScriptName.OnEquipmentUnequipBefore)]
    public static void OnUnequipping()
    {
        var droid = OBJECT_SELF;
        try
        {
            if (!Droid.IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            _unequipSlots[droid] = (item, Item.GetItemSlot(droid, item));
        }
        catch (Exception exception)
        {
            Log.Write(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
        }
    }

    // Native unequips (RunUnequip): unequip actions, inventory drags and queued combat-round unequips.
    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnUnequipped()
    {
        var droid = OBJECT_SELF;
        var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
        var slot = _unequipSlots.Remove(droid, out var pending) && pending.Item == item
            ? pending.Slot
            : InventorySlot.Invalid;
        // An item still in a slot was not unequipped.
        if (Droid.IsDroid(droid) && Item.GetItemSlot(droid, item) == InventorySlot.Invalid)
            Record("unequipped", droid, item, slot);
    }

    /// <summary>
    /// Engine paths such as disarm, polymorph, replacement equips and item level checks change hands
    /// without either event. The heartbeat compares the hands with the last state seen.
    /// </summary>
    [NWNEventHandler(ScriptName.OnDroidHeartbeat)]
    public static void CheckHands()
    {
        var droid = OBJECT_SELF;
        try
        {
            if (!Droid.IsDroid(droid) || Droid.IsSpawning(droid))
                return;

            var hands = DescribeHands(droid);
            if (_lastHands.TryGetValue(droid, out var previous) && previous != hands)
                Write("hands changed without an equip event", droid, OBJECT_INVALID, InventorySlot.Invalid, $"before={previous}");
            _lastHands[droid] = hands;
        }
        catch (Exception exception)
        {
            Log.Write(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
        }
    }

    /// <summary>Sets the hand baseline once a summon has restored the droid's equipment, without logging the restore.</summary>
    public static void RememberHands(uint droid)
    {
        try
        {
            _lastHands[droid] = DescribeHands(droid);
        }
        catch (Exception exception)
        {
            Log.Write(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
        }
    }

    [NWNEventHandler(ScriptName.OnObjectDestroyed)]
    public static void Forget()
    {
        _lastHands.Remove(OBJECT_SELF);
        _unequipSlots.Remove(OBJECT_SELF);
    }

    private static void Record(string change, uint droid, uint item, InventorySlot slot)
    {
        try
        {
            if (!Droid.IsDroid(droid) || Droid.IsSpawning(droid))
                return;

            Write(change, droid, item, slot, string.Empty);
            _lastHands[droid] = DescribeHands(droid);
        }
        catch (Exception exception)
        {
            Log.Write(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
        }
    }

    private static void Write(string change, uint droid, uint item, InventorySlot slot, string detail)
    {
        var fields = new List<(string Name, string Value)>
        {
            ("change", change),
            ("droid", $"{GetName(droid)} ({droid:X})"),
            ("master", GetName(GetMaster(droid)))
        };
        if (GetIsObjectValid(item))
        {
            fields.Add(("item", GetResRef(item)));
            fields.Add(("slot", slot.ToString()));
        }
        if (!string.IsNullOrEmpty(detail))
            fields.Add(("detail", detail));
        fields.Add(("hands", DescribeHands(droid)));
        fields.Add(("action", GetCurrentAction(droid).ToString()));
        fields.Add(("inCombat", GetIsInCombat(droid).ToString()));
        fields.Add(("target", GetName(GetAttackTarget(droid))));
        fields.Add(("associateCommand", GetLastAssociateCommand(droid).ToString()));
        fields.Add(("busy", Activity.GetBusyType(droid).ToString()));
        fields.Add(("area", GetTag(GetArea(droid))));
        fields.Add(("stack", SummarizeStack(Environment.StackTrace)));

        LastEntry = Format(fields);
        Log.Write(LogGroup.DroidEquipment, LastEntry);
    }

    private static string DescribeHands(uint droid) =>
        $"{GetResRef(GetItemInSlot(InventorySlot.RightHand, droid))}|{GetResRef(GetItemInSlot(InventorySlot.LeftHand, droid))}";

    public static string Format(IEnumerable<(string Name, string Value)> fields)
    {
        var parts = new List<string>();
        foreach (var (name, value) in fields)
            parts.Add($"{name}={value}");
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Reduces a managed stack trace to the SWLOR methods that led to the change. An empty result means
    /// no SWLOR code made the change directly: an engine action, the action queue or a client message did.
    /// </summary>
    public static string SummarizeStack(string stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
            return string.Empty;

        var frames = new List<string>();
        foreach (var rawLine in stackTrace.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("at SWLOR.", StringComparison.Ordinal))
                continue;

            var method = line[3..];
            var parameters = method.IndexOf('(');
            if (parameters >= 0)
                method = method[..parameters];

            // Skip this logger and the shared event and script dispatch plumbing.
            if (method.Contains(nameof(DroidEquipmentDiagnostics), StringComparison.Ordinal) ||
                method.StartsWith("SWLOR.Game.Server.Core.", StringComparison.Ordinal) ||
                method.StartsWith("SWLOR.NWN.", StringComparison.Ordinal))
                continue;

            method = method.Replace("SWLOR.Game.Server.", string.Empty, StringComparison.Ordinal);
            if (frames.Count == 0 || frames[^1] != method)
                frames.Add(method);
            if (frames.Count == MaxStackFrames)
                break;
        }

        return string.Join("<", frames);
    }
}
