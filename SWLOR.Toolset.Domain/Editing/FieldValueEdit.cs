using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Memento for a scalar JsonGffField value change (SetString/SetInteger/SetUnsignedInteger/
    /// SetSingle/SetDouble). Restores the exact raw JSON token bytes captured before and after the
    /// mutation, plus the field-level "id" (RawLocStringId) alongside it, so Apply/Revert never
    /// re-derive formatting and always reproduce the original bytes exactly.
    /// </summary>
    public sealed class FieldValueEdit : IDocumentEdit
    {
        private readonly JsonGffField _field;
        private readonly byte[]? _oldValue;
        private readonly byte[]? _oldLocStringId;
        private readonly byte[]? _newValue;
        private readonly byte[]? _newLocStringId;

        internal FieldValueEdit(JsonGffField field, byte[]? oldValue, byte[]? oldLocStringId,
            byte[]? newValue, byte[]? newLocStringId)
        {
            _field = field;
            _oldValue = oldValue;
            _oldLocStringId = oldLocStringId;
            _newValue = newValue;
            _newLocStringId = newLocStringId;
        }

        public void Apply()
        {
            _field.RawValue = _newValue;
            _field.RawLocStringId = _newLocStringId;
        }

        public void Revert()
        {
            _field.RawValue = _oldValue;
            _field.RawLocStringId = _oldLocStringId;
        }

        public string Describe()
        {
            return $"Set {GffFieldTypeNames.NameOf(_field.Type)} field value";
        }
    }

    /// <summary>
    /// Memento for a single LocStringEntry's text change (LocStringEntry.SetText). Restores the
    /// exact raw string token bytes.
    /// </summary>
    public sealed class LocStringEntryTextEdit : IDocumentEdit
    {
        private readonly LocStringEntry _entry;
        private readonly byte[] _oldRawText;
        private readonly byte[] _newRawText;

        internal LocStringEntryTextEdit(LocStringEntry entry, byte[] oldRawText, byte[] newRawText)
        {
            _entry = entry;
            _oldRawText = oldRawText;
            _newRawText = newRawText;
        }

        public void Apply()
        {
            _entry.RawText = _newRawText;
        }

        public void Revert()
        {
            _entry.RawText = _oldRawText;
        }

        public string Describe()
        {
            return $"Set locstring text for language '{_entry.LanguageKey}'";
        }
    }
}
