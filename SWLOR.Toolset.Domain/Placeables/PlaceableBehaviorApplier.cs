using System.Globalization;
using System.Text;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// Switches a placeable from one behavior to another: clears what the old one owned, writes
    /// what the new one needs, and leaves everything else exactly as it was.
    /// </summary>
    /// <remarks>
    /// Callers run this inside a single <c>DocumentTransaction</c> so the whole switch is one undo
    /// step. Nothing here writes a value the game does not read - a behavior is a view, so applying
    /// one only ever touches real script slots, real flags and real local variables.
    /// </remarks>
    public static class PlaceableBehaviorApplier
    {
        /// <summary>
        /// Values the previous behavior owned that carry data and would be replaced or cleared by
        /// switching. The editor names these in its confirmation rather than discarding silently.
        /// </summary>
        public static IReadOnlyList<string> ValuesLostBySwitching(
            JsonGffStruct root, PlaceableBehavior from, PlaceableBehavior to)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(to);

            var table = new VarTable(root);
            var kept = new HashSet<string>(
                to.Fields
                    .Where(field => field.IsVisible || AlreadyHasFixedValue(table, field))
                    .Select(field => field.VariableName),
                StringComparer.Ordinal);
            var losses = new List<string>();

            if (ReferenceEquals(from, PlaceableBehaviorCatalog.Custom))
            {
                foreach (var slot in PlaceableBehaviorDetector.ReadScripts(root))
                {
                    var targetKeepsValue = to.Scripts.TryGetValue(slot.Key, out var targetScript) &&
                                           string.Equals(
                                               slot.Value,
                                               targetScript,
                                               StringComparison.OrdinalIgnoreCase);
                    if (!targetKeepsValue)
                        losses.Add($"{slot.Key} script");
                }

                losses.AddRange(table
                    .Select(entry => entry.Name)
                    .Where(name => !kept.Contains(name))
                    .Where(name => HasValue(table, name)));

                return losses;
            }

            losses.AddRange(from.Fields
                .Where(field => !kept.Contains(field.VariableName))
                .Where(field => HasAuthoredValue(table, field))
                .Select(field => field.VariableName));
            return losses;
        }

        /// <summary>
        /// Values that would be lost and differ from the form's baseline. Existing saved
        /// configuration is not an unsaved entry, so merely opening a placeable and choosing a
        /// different behavior must not raise a discard warning.
        /// </summary>
        public static IReadOnlyList<string> UnsavedValuesLostBySwitching(
            JsonGffStruct root,
            JsonGffStruct baseline,
            PlaceableBehavior from,
            PlaceableBehavior to)
        {
            ArgumentNullException.ThrowIfNull(baseline);

            var currentVariables = new VarTable(root);
            var baselineVariables = new VarTable(baseline);
            var kept = new HashSet<string>(
                to.Fields
                    .Where(field => field.IsVisible || AlreadyHasFixedValue(currentVariables, field))
                    .Select(field => field.VariableName),
                StringComparer.Ordinal);
            var losses = new List<string>();

            if (ReferenceEquals(from, PlaceableBehaviorCatalog.Custom))
            {
                foreach (var slot in PlaceableBehaviorDetector.ReadScripts(root))
                {
                    var targetKeepsValue = to.Scripts.TryGetValue(slot.Key, out var targetScript) &&
                                           string.Equals(
                                               slot.Value,
                                               targetScript,
                                               StringComparison.OrdinalIgnoreCase);
                    var changed = !string.Equals(
                        slot.Value,
                        baseline.GetStringOrNull(slot.Key),
                        StringComparison.Ordinal);
                    if (!targetKeepsValue && changed)
                        losses.Add($"{slot.Key} script");
                }

                losses.AddRange(currentVariables
                    .Select(entry => entry.Name)
                    .Where(name => !kept.Contains(name))
                    .Where(name => !SameLocalValue(currentVariables, baselineVariables, name)));
                return losses;
            }

            losses.AddRange(from.Fields
                .Where(field => !kept.Contains(field.VariableName))
                .Where(field => HasEntry(currentVariables, field.VariableName))
                .Where(field => !SameLocalValue(
                    currentVariables,
                    baselineVariables,
                    field.VariableName))
                .Select(field => field.VariableName));
            return losses;
        }

        /// <summary>Applies <paramref name="to"/>, having previously been <paramref name="from"/>.</summary>
        public static void Apply(JsonGffStruct root, PlaceableBehavior from, PlaceableBehavior to)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(to);

            if (ReferenceEquals(from, to))
                return;

            ClearOwnedScripts(root, from, to);
            ClearOwnedVariables(root, from, to);
            ClearOwnedFlags(root, from, to);

            EnsureExpectedValues(root, to);
        }

        /// <summary>
        /// Writes the selected named behavior's canonical scripts, required root flags, and missing
        /// defaults. User-authored visible field values and behavior-editable flags are preserved.
        /// Sentinels have no implementation wiring to materialize.
        /// </summary>
        public static void EnsureExpectedValues(JsonGffStruct root, PlaceableBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(behavior);

            if (behavior.IsSentinel)
                return;

            foreach (var slot in behavior.Scripts)
                SetString(root, slot.Key, slot.Value, GffFieldType.ResRef);

            foreach (var flag in behavior.Flags)
                SetInteger(root, flag.FieldName, flag.Value ? 1 : 0, GffFieldType.Byte);

            WriteDefaults(root, behavior);
        }

        /// <summary>
        /// Whether <see cref="EnsureExpectedValues"/> would change the selected named behavior's
        /// canonical scripts, required flags, or defaults. Visible authored values are intentionally
        /// accepted exactly as <see cref="WriteDefaults"/> accepts them.
        /// </summary>
        public static bool NeedsExpectedValues(JsonGffStruct root, PlaceableBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(behavior);

            if (behavior.IsSentinel)
                return false;

            if (behavior.Scripts.Any(slot =>
                    !string.Equals(
                        root.GetStringOrNull(slot.Key),
                        slot.Value,
                        StringComparison.Ordinal)))
            {
                return true;
            }

            if (behavior.Flags.Any(flag =>
                    root.GetIntOrNull(flag.FieldName) != (flag.Value ? 1 : 0)))
            {
                return true;
            }

            var table = new VarTable(root);
            foreach (var field in behavior.Fields)
            {
                if (field.DefaultIntValue == null && string.IsNullOrWhiteSpace(field.DefaultStringValue))
                    continue;

                var entry = table.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, field.VariableName, StringComparison.Ordinal));
                if (entry != null && field.IsVisible)
                    continue;

                if (field.DefaultIntValue is { } intValue)
                {
                    if (entry?.Type != VarTable.TypeInt || entry.IntValue != intValue)
                        return true;
                }
                else if (entry?.Type != VarTable.TypeString ||
                         !string.Equals(
                             entry.StringValue,
                             field.DefaultStringValue,
                             StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a script slot is written by a named behavior rather than by hand. Custom exposes
        /// the raw slot beneath its flags; named behaviors keep their own wiring authoritative.
        /// </summary>
        public static bool OwnsScriptSlot(PlaceableBehavior behavior, string slot) =>
            behavior.Scripts.ContainsKey(slot);

        private static void ClearOwnedScripts(JsonGffStruct root, PlaceableBehavior from, PlaceableBehavior to)
        {
            if (ReferenceEquals(from, PlaceableBehaviorCatalog.Custom))
            {
                foreach (var slot in PlaceableBehaviorDetector.ScriptSlots)
                {
                    if (!to.Scripts.ContainsKey(slot) && root.GetStringOrNull(slot) != null)
                        SetString(root, slot, string.Empty, GffFieldType.ResRef);
                }

                return;
            }

            // Custom is "stop interpreting this, show me the raw wiring". Erasing the wiring on the
            // way in is backwards: a Chair switched to Custom opened the script panel with its
            // OnUsed already blank, so the panel that exists to reveal what is there revealed
            // nothing, and the loss was silent because no target behavior was replacing it. Flags
            // are already exempt for exactly this reason; scripts belong with them.
            if (ReferenceEquals(to, PlaceableBehaviorCatalog.Custom))
                return;

            foreach (var slot in from.Scripts)
            {
                if (to.Scripts.ContainsKey(slot.Key))
                    continue;

                var current = root.GetStringOrNull(slot.Key);
                if (current == null)
                    continue;

                // Only clear the script this behavior put there. A slot a builder edited by hand
                // is theirs, not ours.
                var isOwned = string.Equals(current, slot.Value, StringComparison.OrdinalIgnoreCase) ||
                              from.AlternateScripts.Contains(current, StringComparer.OrdinalIgnoreCase);
                if (isOwned)
                    SetString(root, slot.Key, string.Empty, GffFieldType.ResRef);
            }
        }

        private static void ClearOwnedVariables(JsonGffStruct root, PlaceableBehavior from, PlaceableBehavior to)
        {
            var kept = new HashSet<string>(to.VariableNames, StringComparer.Ordinal);
            var table = new VarTable(root);

            if (ReferenceEquals(from, PlaceableBehaviorCatalog.Custom))
            {
                foreach (var name in table.Select(entry => entry.Name).ToList())
                {
                    if (!kept.Contains(name))
                        table.Remove(name);
                }

                return;
            }

            foreach (var name in from.VariableNames)
            {
                if (!kept.Contains(name))
                    table.Remove(name);
            }
        }

        private static void ClearOwnedFlags(JsonGffStruct root, PlaceableBehavior from, PlaceableBehavior to)
        {
            // Custom flags are explicitly hand-authored. A named behavior may add what it requires,
            // but must not silently erase the other choices when the builder leaves Custom.
            if (ReferenceEquals(from, PlaceableBehaviorCatalog.Custom) ||
                ReferenceEquals(to, PlaceableBehaviorCatalog.Custom))
                return;

            var kept = new HashSet<string>(
                to.Flags.Select(flag => flag.FieldName),
                StringComparer.Ordinal);

            foreach (var flag in from.Flags)
            {
                if (kept.Contains(flag.FieldName))
                    continue;

                var field = root.GetOrNull(flag.FieldName);
                if (field?.GetInteger() == (flag.Value ? 1 : 0))
                    SetInteger(root, flag.FieldName, 0, GffFieldType.Byte);
            }
        }

        private static void WriteDefaults(JsonGffStruct root, PlaceableBehavior behavior)
        {
            var table = new VarTable(root);
            foreach (var field in behavior.Fields)
            {
                var hasExisting = table.Any(entry =>
                    string.Equals(entry.Name, field.VariableName, StringComparison.Ordinal));
                if (hasExisting && field.IsVisible)
                {
                    continue;
                }

                if (field.DefaultIntValue is { } intValue)
                    table.SetInt(field.VariableName, intValue);
                else if (!string.IsNullOrWhiteSpace(field.DefaultStringValue))
                    table.SetString(field.VariableName, field.DefaultStringValue);
            }
        }

        private static bool HasValue(VarTable table, string name)
        {
            var entry = table.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.Ordinal));

            if (entry == null)
                return false;

            return entry.Type switch
            {
                VarTable.TypeString => !string.IsNullOrEmpty(entry.StringValue),
                VarTable.TypeInt => entry.IntValue is not (null or 0),
                VarTable.TypeFloat => entry.FloatValue is not (null or 0f),
                _ => true
            };
        }

        private static bool SameLocalValue(VarTable current, VarTable baseline, string name)
        {
            var currentEntry = current.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.Ordinal));
            var baselineEntry = baseline.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.Ordinal));

            if (currentEntry == null || baselineEntry == null)
                return currentEntry == null && baselineEntry == null;
            if (currentEntry.Type != baselineEntry.Type)
                return false;

            return currentEntry.Type switch
            {
                VarTable.TypeInt => currentEntry.IntValue == baselineEntry.IntValue,
                VarTable.TypeFloat => currentEntry.FloatValue == baselineEntry.FloatValue,
                VarTable.TypeString => string.Equals(
                    currentEntry.StringValue,
                    baselineEntry.StringValue,
                    StringComparison.Ordinal),
                _ => true
            };
        }

        private static bool HasEntry(VarTable table, string name) =>
            table.Any(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));

        /// <summary>
        /// A value that differs from the behavior's initial value is authored configuration worth
        /// warning about. Defaults written merely by selecting a behavior are replaceable setup,
        /// not an unsaved decision by the builder.
        /// </summary>
        private static bool HasAuthoredValue(VarTable table, PlaceableBehaviorField field)
        {
            var entry = table.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, field.VariableName, StringComparison.Ordinal));
            if (entry == null || !HasValue(table, field.VariableName))
                return false;

            if (field.DefaultIntValue is { } intValue &&
                entry.Type == VarTable.TypeInt &&
                entry.IntValue == intValue)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(field.DefaultStringValue) &&
                entry.Type == VarTable.TypeString &&
                string.Equals(
                    entry.StringValue,
                    field.DefaultStringValue,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private static bool AlreadyHasFixedValue(VarTable table, PlaceableBehaviorField field)
        {
            if (field.IsVisible)
                return true;

            var entry = table.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, field.VariableName, StringComparison.Ordinal));
            if (entry == null)
                return false;

            if (field.DefaultIntValue is { } intValue)
                return entry.IntValue == intValue;

            return !string.IsNullOrWhiteSpace(field.DefaultStringValue) &&
                   string.Equals(
                       entry.StringValue,
                       field.DefaultStringValue,
                       StringComparison.Ordinal);
        }

        private static void SetString(JsonGffStruct root, string fieldName, string value, GffFieldType type)
        {
            var field = root.GetOrNull(fieldName);
            if (field == null)
            {
                root.Add(fieldName, JsonGffField.CreateScalar(type, JsonStringCodec.Encode(value)));
                return;
            }

            if (string.Equals(field.GetString(), value, StringComparison.Ordinal))
                return;

            field.SetString(value);
        }

        private static void SetInteger(JsonGffStruct root, string fieldName, long value, GffFieldType type)
        {
            var field = root.GetOrNull(fieldName);
            if (field == null)
            {
                var raw = Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
                root.Add(fieldName, JsonGffField.CreateScalar(type, raw));
                return;
            }

            if (field.GetInteger() == value)
                return;

            field.SetInteger(value);
        }
    }
}
