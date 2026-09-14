using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors
{
    /// <summary>One dropdown-backed field whose stored value has no matching entry in its lookup.</summary>
    /// <param name="Label">The field's editor label (e.g. "Appearance").</param>
    /// <param name="FieldName">The GFF field name (e.g. "Appearance").</param>
    /// <param name="LookupKey">The lookup the value was checked against (e.g. "placeables").</param>
    /// <param name="Value">The value actually stored in the document.</param>
    public sealed record UnresolvedFieldValue(string Label, string FieldName, string LookupKey, long Value);

    /// <summary>
    /// Checks a document's dropdown-backed fields against their lookup tables before an editor is
    /// opened.
    ///
    /// This exists because a dropdown can only offer values its lookup knows about. When a document
    /// stores an id the lookup has no row for, the combo box has nothing to select and renders
    /// blank - the real value becomes invisible, and a user who then touches that field overwrites
    /// it without ever seeing what was there. Rather than guess a fallback or silently show an empty
    /// box, the editor refuses to open and reports exactly which values it could not resolve, so the
    /// underlying data is left untouched.
    ///
    /// Real example from this repo: placeables.2da row 1005 is entirely "****" (no label, no model),
    /// yet ~2900 placeable blueprints reference appearances like it.
    /// </summary>
    public static class DropdownValueValidator
    {
        /// <summary>
        /// Every dropdown field in <paramref name="schema"/> whose value in
        /// <paramref name="document"/> is absent from its lookup.
        ///
        /// <paramref name="validIdsFor"/> supplies the ids a lookup key can offer. An EMPTY result
        /// means the lookup is unavailable (its 2DA or service did not load), which is not an error:
        /// the editor already degrades those fields to a plain numeric box that shows and preserves
        /// the raw value, so nothing can be lost and nothing is reported. A field absent from the
        /// document is likewise skipped - there is no value at risk.
        /// </summary>
        public static IReadOnlyList<UnresolvedFieldValue> FindUnresolved(
            JsonGffDocument document,
            EditorSchema schema,
            Func<string, IReadOnlyCollection<long>> validIdsFor)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(validIdsFor);

            var unresolved = new List<UnresolvedFieldValue>();

            foreach (var field in schema.AllFields)
            {
                if (field.Kind != EditorKind.TwoDaDropdown || string.IsNullOrEmpty(field.LookupKey))
                    continue;

                if (!document.Root.TryGet(field.FieldName, out var gffField))
                    continue; // nothing stored - no value to lose

                long value;
                try
                {
                    value = gffField.GetInteger();
                }
                catch (Exception)
                {
                    continue; // not an integer field; the dropdown never drives it
                }

                if (IsUnsetSentinel(value, field.FieldType))
                    continue; // "none", not a broken reference

                var validIds = validIdsFor(field.LookupKey);
                if (validIds.Count == 0)
                    continue; // lookup unavailable - field falls back to a numeric box, which is safe

                if (!validIds.Contains(value))
                    unresolved.Add(new UnresolvedFieldValue(field.Label, field.FieldName, field.LookupKey, value));
            }

            return unresolved;
        }

        /// <summary>
        /// Whether a value is NWN's "nothing assigned" marker for its field width rather than a
        /// broken row reference: the type's all-bits-set value (255 / 65535 / 4294967295) or -1.
        ///
        /// This matters in the real corpus - 45 creature blueprints store SoundSetFile = 65535,
        /// which simply means "no sound set", and refusing to open all of them would be a bug in
        /// this guard rather than a defect in the data. Zero is deliberately NOT treated as unset:
        /// row 0 is a real row in every table wired to a dropdown.
        /// </summary>
        public static bool IsUnsetSentinel(long value, GffFieldType fieldType)
        {
            if (value == -1)
                return true;

            return fieldType switch
            {
                GffFieldType.Byte => value == byte.MaxValue,
                GffFieldType.Word => value == ushort.MaxValue,
                GffFieldType.Dword => value == uint.MaxValue,
                GffFieldType.Short => value == short.MaxValue,
                _ => false
            };
        }

        /// <summary>
        /// The canonical "nothing assigned" value a populated dropdown should expose for this
        /// GFF field width. Unsigned fields use their all-bits-set value; signed fields use -1.
        /// </summary>
        public static long GetUnsetSentinel(GffFieldType fieldType) => fieldType switch
        {
            GffFieldType.Byte => byte.MaxValue,
            GffFieldType.Word => ushort.MaxValue,
            GffFieldType.Dword => uint.MaxValue,
            GffFieldType.Char or GffFieldType.Short or GffFieldType.Int or GffFieldType.Int64 => -1,
            _ => -1
        };
    }
}
