using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Logs equipment changes on player droids outside their summon restore: the logic and state that used to be
    /// static in <c>Service.DroidEquipmentDiagnostics</c>, moved here unchanged. The facade still carries the event
    /// handlers and forwards every call to the container's instance.
    /// </summary>
    public class DroidEquipmentDiagnosticsService : IDroidEquipmentDiagnosticsService
    {
        private const int MaxStackFrames = 8;
        private readonly Dictionary<uint, string> _lastHands = new();
        private readonly Dictionary<uint, (uint Item, InventorySlot Slot)> _unequipSlots = new();

        public string LastEntry { get; private set; } = string.Empty;

        public void OnEquipped()
        {
            var droid = OBJECT_SELF;
            if (!Droid.IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var slot = Item.GetItemSlot(droid, item);
            if (slot != InventorySlot.Invalid)
                Record("equipped", droid, item, slot);
        }

        public void OnUnequipping()
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
                WriteLog(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
            }
        }

        public void OnUnequipped()
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

        public void CheckHands()
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
                WriteLog(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
            }
        }

        public void RememberHands(uint droid)
        {
            try
            {
                _lastHands[droid] = DescribeHands(droid);
            }
            catch (Exception exception)
            {
                WriteLog(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
            }
        }

        public void Forget()
        {
            _lastHands.Remove(OBJECT_SELF);
            _unequipSlots.Remove(OBJECT_SELF);
        }

        private void Record(string change, uint droid, uint item, InventorySlot slot)
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
                WriteLog(LogGroup.Error, $"Droid equipment diagnostics failed: {exception.Message}");
            }
        }

        private void Write(string change, uint droid, uint item, InventorySlot slot, string detail)
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
            fields.Add(("stack", SummarizeStack(GetStackTrace())));

            LastEntry = Format(fields);
            WriteLog(LogGroup.DroidEquipment, LastEntry);
        }

        // The object id tells apart two copies of the same blueprint swapped into a hand.
        private string DescribeHands(uint droid) =>
            $"{DescribeItem(GetItemInSlot(InventorySlot.RightHand, droid))}|{DescribeItem(GetItemInSlot(InventorySlot.LeftHand, droid))}";

        private string DescribeItem(uint item) =>
            GetIsObjectValid(item) ? $"{GetResRef(item)}#{item:X}" : string.Empty;

        public string Format(IEnumerable<(string Name, string Value)> fields)
        {
            var parts = new List<string>();
            foreach (var (name, value) in fields)
                parts.Add($"{name}={value}");
            return string.Join(" ", parts);
        }

        public string SummarizeStack(string stackTrace)
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

                // Skip this logger and the shared event and script dispatch plumbing. The facade and this class both
                // carry the name of the service.
                if (method.Contains("DroidEquipmentDiagnostics", StringComparison.Ordinal) ||
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

        /// <summary>
        /// The managed stack of the call that changed the equipment. The original call, on its own so a unit test can
        /// answer it and read the summary it produces.
        /// </summary>
        protected virtual string GetStackTrace()
        {
            return Environment.StackTrace;
        }

        /// <summary>
        /// Writes one line to the log. The original call into <c>Log</c> (which is not converted yet and writes to the
        /// configured log files), on its own so a unit test can read what was logged.
        /// </summary>
        protected virtual void WriteLog(LogGroup group, string details)
        {
            Log.Write(group, details);
        }
    }
}
