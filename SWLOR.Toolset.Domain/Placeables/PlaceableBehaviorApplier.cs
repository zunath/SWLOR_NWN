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

            foreach (var slot in to.Scripts)
                SetString(root, slot.Key, slot.Value, GffFieldType.ResRef);

            foreach (var flag in to.Flags)
                SetInteger(root, flag.FieldName, flag.Value ? 1 : 0, GffFieldType.Byte);

            WriteDefaults(root, to);
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

            field.SetInteger(value);
        }
    }
}
