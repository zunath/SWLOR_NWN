using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>
    /// What switching away from a behavior will actually throw away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applying a behavior clears what the previous one owned before writing the new one's values.
    /// For a named behavior that is unremarkable: the values being cleared are the ones that
    /// behavior itself wrote, and the incoming behavior replaces them. For <em>Custom</em> it is not.
    /// Custom owns every raw slot the object has — including scripts and locals a builder wired by
    /// hand for reasons the toolset knows nothing about — and switching to a preset erases all of
    /// them at once.
    /// </para>
    /// <para>
    /// Both the door and the trigger editor had that switch with no confirmation in front of it, and
    /// the loss only becomes visible after the document is saved. This is the list to put in front
    /// of the builder first: what is named here is what will be gone.
    /// </para>
    /// </remarks>
    public static class BehaviorSwitchLosses
    {
        /// <summary>
        /// Values the outgoing behavior owns that hold something and will not be written back by the
        /// incoming one.
        /// </summary>
        /// <param name="store">The object being edited.</param>
        /// <param name="manages">The outgoing behavior's pinned values.</param>
        /// <param name="fields">The outgoing behavior's editable fields.</param>
        /// <param name="incoming">
        /// What the new behavior will write. A value on this list is being replaced rather than lost,
        /// so it is not worth stopping the builder over.
        /// </param>
        /// <param name="extraLocals">
        /// Locals cleared beyond the named fields — Custom sweeps the whole table, and a behavior
        /// with owned prefixes sweeps everything under them.
        /// </param>
        public static IReadOnlyList<string> Describe(
            BehaviorValueStore store,
            IEnumerable<BehaviorManagedValue> manages,
            IEnumerable<BehaviorFieldDefinition> fields,
            IEnumerable<BehaviorManagedValue> incoming,
            IEnumerable<string>? extraLocals = null)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(manages);
            ArgumentNullException.ThrowIfNull(fields);
            ArgumentNullException.ThrowIfNull(incoming);

            var replaced = new HashSet<string>(
                incoming.Select(value => value.Name),
                StringComparer.Ordinal);
            var losses = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var value in manages)
            {
                if (!value.ClearOnSwap || replaced.Contains(value.Name))
                    continue;

                // A pinned value that still holds exactly what its behavior wrote is the editor's
                // own footprint, not the builder's work. Choosing Area Transition writes Cursor and
                // choosing anything else takes it away again; stopping to ask about that is asking
                // the builder to approve undoing a change they never made.
                if (store.Matches(value))
                    continue;

                Consider(value.Storage, value.Name, value.FieldType);
            }

            foreach (var field in fields)
            {
                if (field.Name.Length == 0 || replaced.Contains(field.Name))
                    continue;

                Consider(field.Storage, field.Name, field.FieldType);
            }

            foreach (var name in extraLocals ?? Array.Empty<string>())
            {
                if (!replaced.Contains(name))
                    Consider(BehaviorFieldStorage.Local, name, GffFieldType.CExoString);
            }

            return losses;

            void Consider(BehaviorFieldStorage storage, string name, GffFieldType type)
            {
                if (!seen.Add(name) || !HoldsSomething(store, storage, name, type))
                    return;

                losses.Add(name);
            }
        }

        /// <summary>
        /// Whether a slot actually carries data. An empty script slot or a zeroed flag is not a loss,
        /// and listing it would bury the one line that matters among a dozen that do not.
        /// </summary>
        private static bool HoldsSomething(
            BehaviorValueStore store, BehaviorFieldStorage storage, string name, GffFieldType type)
        {
            if (storage == BehaviorFieldStorage.Local)
                return store.Locals.Any(entry =>
                    string.Equals(entry.Name, name, StringComparison.Ordinal));

            return type switch
            {
                GffFieldType.CExoLocString =>
                    !string.IsNullOrWhiteSpace(store.GetLocalizedText(name)) ||
                    store.GetLocalizedStringRef(name) != null,
                GffFieldType.ResRef or GffFieldType.CExoString =>
                    !string.IsNullOrWhiteSpace(store.GetString(storage, name)),
                GffFieldType.Float => store.GetFloat(storage, name) is not (null or 0d),
                GffFieldType.List => store.ValueStruct.GetOrNull(name)?.Elements is { Count: > 0 },
                _ => store.GetInteger(storage, name) is not (null or 0)
            };
        }
    }
}
