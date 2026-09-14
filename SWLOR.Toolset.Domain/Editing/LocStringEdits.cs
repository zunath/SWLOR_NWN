using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Memento for inserting a language entry into a cexolocstring field
    /// (JsonGffField.InsertLocStringEntry / AddLocStringEntry).
    /// </summary>
    public sealed class InsertLocStringEntryEdit : IDocumentEdit
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly LocStringEntry _entry;

        internal InsertLocStringEntryEdit(JsonGffField field, int index, LocStringEntry entry)
        {
            _field = field;
            _index = index;
            _entry = entry;
        }

        public void Apply()
        {
            _field.InsertLocStringEntry(_index, _entry);
        }

        public void Revert()
        {
            _field.RemoveLocStringEntry(_entry.LanguageKey);
        }

        public string Describe()
        {
            return $"Add locstring entry '{_entry.LanguageKey}'";
        }
    }

    /// <summary>
    /// Memento for removing a language entry from a cexolocstring field
    /// (JsonGffField.RemoveLocStringEntry). Reverting re-inserts at the original index so
    /// untouched entries' relative order (and therefore serialized bytes) is reproduced exactly.
    /// </summary>
    public sealed class RemoveLocStringEntryEdit : IDocumentEdit
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly LocStringEntry _entry;

        internal RemoveLocStringEntryEdit(JsonGffField field, int index, LocStringEntry entry)
        {
            _field = field;
            _index = index;
            _entry = entry;
        }

        public void Apply()
        {
            _field.RemoveLocStringEntry(_entry.LanguageKey);
        }

        public void Revert()
        {
            _field.InsertLocStringEntry(_index, _entry);
        }

        public string Describe()
        {
            return $"Remove locstring entry '{_entry.LanguageKey}'";
        }
    }

    /// <summary>
    /// Memento for replacing a cexolocstring field's complete value — its strref and its whole
    /// entry list — with a copy of another localized string (LocString.CopyFrom).
    /// </summary>
    public sealed class LocStringReplaceEdit : IDocumentEdit
    {
        private readonly JsonGffField _field;
        private readonly byte[]? _oldLocStringId;
        private readonly List<LocStringEntry>? _oldEntries;
        private readonly byte[]? _newLocStringId;
        private readonly List<LocStringEntry>? _newEntries;

        internal LocStringReplaceEdit(
            JsonGffField field,
            byte[]? oldLocStringId, List<LocStringEntry>? oldEntries,
            byte[]? newLocStringId, List<LocStringEntry>? newEntries)
        {
            _field = field;
            _oldLocStringId = oldLocStringId;
            _oldEntries = oldEntries;
            _newLocStringId = newLocStringId;
            _newEntries = newEntries;
        }

        public void Apply()
        {
            _field.RawLocStringId = _newLocStringId;
            _field.LocStringEntries = _newEntries;
        }

        public void Revert()
        {
            _field.RawLocStringId = _oldLocStringId;
            _field.LocStringEntries = _oldEntries;
        }

        public string Describe()
        {
            return "Replace localized string";
        }
    }
}
