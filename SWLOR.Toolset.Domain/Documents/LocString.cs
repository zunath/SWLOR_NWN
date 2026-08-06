using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// A thin view over a cexolocstring field: the field-level strref ("id"), and the
    /// language-0 (English) text entry that every named locstring property in this package
    /// exposes. Untouched language entries other than "0" are left alone.
    /// </summary>
    public sealed class LocString
    {
        private const string DefaultLanguageKey = "0";

        private readonly JsonGffField _field;

        internal LocString(JsonGffField field)
        {
            if (field.Type != GffFieldType.CExoLocString)
                throw new ArgumentException($"Field type {field.Type} is not a CExoLocString.", nameof(field));

            _field = field;
        }

        /// <summary>The field-level "id" (strref), if the source file recorded one.</summary>
        public uint? StrRef => _field.GetLocStringId();

        /// <summary>The language-0 (English) text, or null if no such entry exists.</summary>
        public string? Text
        {
            get => FindEntry(DefaultLanguageKey)?.GetText();
            set
            {
                _field.ClearLocStringId();
                GetOrAddEntry(DefaultLanguageKey).SetText(value ?? string.Empty);
            }
        }

        /// <summary>The text for an arbitrary language key, or null if no such entry exists.</summary>
        public string? GetText(string languageKey)
        {
            return FindEntry(languageKey)?.GetText();
        }

        /// <summary>Sets the text for an arbitrary language key, creating the entry if needed.</summary>
        public void SetText(string languageKey, string value)
        {
            _field.ClearLocStringId();
            GetOrAddEntry(languageKey).SetText(value);
        }

        /// <summary>
        /// Replaces this value with an independent copy of another complete localized string,
        /// including its strref and every language/gender entry.
        /// </summary>
        internal void CopyFrom(LocString source)
        {
            ArgumentNullException.ThrowIfNull(source);

            EditScope.EnsureMutationAllowed();
            var oldLocStringId = _field.RawLocStringId;
            var oldEntries = _field.LocStringEntries;
            _field.RawLocStringId = source._field.RawLocStringId?.ToArray();
            _field.LocStringEntries = source._field.LocStringEntries?
                .Select(entry => new LocStringEntry(
                    entry.LanguageKey,
                    entry.RawText.ToArray()))
                .ToList();
            EditScope.Capture(new LocStringReplaceEdit(
                _field, oldLocStringId, oldEntries, _field.RawLocStringId, _field.LocStringEntries));
        }

        private LocStringEntry? FindEntry(string languageKey)
        {
            if (_field.LocStringEntries == null)
                return null;

            foreach (var entry in _field.LocStringEntries)
            {
                if (entry.LanguageKey == languageKey)
                    return entry;
            }

            return null;
        }

        private LocStringEntry GetOrAddEntry(string languageKey)
        {
            _field.LocStringEntries ??= new List<LocStringEntry>();
            var existing = FindEntry(languageKey);
            if (existing != null)
                return existing;

            var entry = new LocStringEntry(languageKey, Array.Empty<byte>());
            _field.AddLocStringEntry(entry);
            return entry;
        }
    }
}
