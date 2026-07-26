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
        /// Variables the previous behavior owned that carry a value, and so would be lost by
        /// switching. The editor names these in its confirmation rather than discarding silently.
        /// </summary>
        public static IReadOnlyList<string> ValuesLostBySwitching(
            JsonGffStruct root, PlaceableBehavior from, PlaceableBehavior to)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(to);

            var kept = new HashSet<string>(to.VariableNames, StringComparer.Ordinal);
            var table = new VarTable(root);

            return from.VariableNames
                .Where(name => !kept.Contains(name))
                .Where(name => HasValue(table, name))
                .ToList();
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

            foreach (var slot in to.Scripts)
                SetString(root, slot.Key, slot.Value, GffFieldType.ResRef);

            foreach (var flag in to.Flags)
                SetInteger(root, flag.FieldName, flag.Value ? 1 : 0, GffFieldType.Byte);
        }

        /// <summary>
        /// Whether a script slot is written by the behavior rather than by hand. The Advanced tab
        /// locks these, and unlocking is what the module's 53 one-off script sets need.
        /// </summary>
        public static bool OwnsScriptSlot(PlaceableBehavior behavior, string slot) =>
            behavior.Scripts.ContainsKey(slot);

        private static void ClearOwnedScripts(JsonGffStruct root, PlaceableBehavior from, PlaceableBehavior to)
        {
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

            foreach (var name in from.VariableNames)
            {
                if (!kept.Contains(name))
                    table.Remove(name);
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
