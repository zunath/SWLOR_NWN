using System.Globalization;
using System.Text;
using SWLOR.Toolset.Domain.Editing;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// One localized-string entry of a cexolocstring value: a language key (e.g. "0") and the
    /// raw string token holding its text.
    /// </summary>
    public sealed class LocStringEntry
    {
        public string LanguageKey { get; }
        public byte[] RawText { get; internal set; }

        public LocStringEntry(string languageKey, byte[] rawText)
        {
            LanguageKey = languageKey;
            RawText = rawText;
        }

        public string GetText()
        {
            return JsonStringCodec.Decode(RawText);
        }

        public void SetText(string text)
        {
            EditScope.EnsureMutationAllowed();
            var oldRawText = RawText;
            RawText = JsonStringCodec.Encode(text);
            EditScope.Capture(new LocStringEntryTextEdit(this, oldRawText, RawText));
        }
    }

    /// <summary>
    /// A single GFF field as stored in nwn_gff JSON. Scalar values keep the exact raw bytes of
    /// their JSON token so untouched fields serialize byte-identically; mutation replaces the
    /// raw token with a freshly formatted one.
    /// </summary>
    public sealed class JsonGffField
    {
        public GffFieldType Type { get; }

        /// <summary>Raw number token of the field-level "id" (cexolocstring strref), if present.</summary>
        public byte[]? RawLocStringId { get; internal set; }

        /// <summary>Raw number token of the field-level "__struct_id" (struct fields), if present.</summary>
        public byte[]? RawFieldStructId { get; internal set; }

        /// <summary>Raw JSON token for scalar values (numbers include digits only; strings include quotes).</summary>
        public byte[]? RawValue { get; internal set; }

        /// <summary>Child struct for struct-typed fields.</summary>
        public JsonGffStruct? Struct { get; internal set; }

        /// <summary>Child structs for list-typed fields.</summary>
        public List<JsonGffStruct>? Elements { get; internal set; }

        /// <summary>Ordered language entries for cexolocstring fields.</summary>
        public List<LocStringEntry>? LocStringEntries { get; internal set; }

        internal JsonGffField(GffFieldType type)
        {
            Type = type;
        }

        public static JsonGffField CreateScalar(GffFieldType type, byte[] rawValue)
        {
            if (!GffFieldTypeNames.IsNumeric(type) && !GffFieldTypeNames.IsString(type))
                throw new ArgumentException($"{type} is not a scalar field type.", nameof(type));

            return new JsonGffField(type) { RawValue = rawValue };
        }

        public static JsonGffField CreateStruct(uint structId)
        {
            var raw = Encoding.ASCII.GetBytes(structId.ToString(CultureInfo.InvariantCulture));
            return new JsonGffField(GffFieldType.Struct)
            {
                RawFieldStructId = raw,
                Struct = new JsonGffStruct { RawStructId = raw }
            };
        }

        public static JsonGffField CreateList()
        {
            return new JsonGffField(GffFieldType.List) { Elements = new List<JsonGffStruct>() };
        }

        public static JsonGffField CreateLocString()
        {
            return new JsonGffField(GffFieldType.CExoLocString) { LocStringEntries = new List<LocStringEntry>() };
        }

        public string GetString()
        {
            RequireScalar();
            if (!GffFieldTypeNames.IsString(Type))
                throw new InvalidOperationException($"Field type {Type} does not hold a string value.");

            return JsonStringCodec.Decode(RawValue);
        }

        public void SetString(string value)
        {
            RequireScalar();
            if (!GffFieldTypeNames.IsString(Type))
                throw new InvalidOperationException($"Field type {Type} does not hold a string value.");

            EditScope.EnsureMutationAllowed();
            var oldValue = RawValue;
            var oldLocId = RawLocStringId;
            RawValue = JsonStringCodec.Encode(value);
            EditScope.Capture(new FieldValueEdit(this, oldValue, oldLocId, RawValue, RawLocStringId));
        }

        public long GetInteger()
        {
            RequireScalar();
            return long.Parse(RawText(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public ulong GetUnsignedInteger()
        {
            RequireScalar();
            return ulong.Parse(RawText(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public void SetInteger(long value)
        {
            RequireScalar();
            if (Type is GffFieldType.Float or GffFieldType.Double || !GffFieldTypeNames.IsNumeric(Type))
                throw new InvalidOperationException($"Field type {Type} does not hold an integer value.");

            EditScope.EnsureMutationAllowed();
            var oldValue = RawValue;
            var oldLocId = RawLocStringId;
            RawValue = Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
            EditScope.Capture(new FieldValueEdit(this, oldValue, oldLocId, RawValue, RawLocStringId));
        }

        public void SetUnsignedInteger(ulong value)
        {
            RequireScalar();
            if (Type is GffFieldType.Float or GffFieldType.Double || !GffFieldTypeNames.IsNumeric(Type))
                throw new InvalidOperationException($"Field type {Type} does not hold an integer value.");

            EditScope.EnsureMutationAllowed();
            var oldValue = RawValue;
            var oldLocId = RawLocStringId;
            RawValue = Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
            EditScope.Capture(new FieldValueEdit(this, oldValue, oldLocId, RawValue, RawLocStringId));
        }

        public double GetDouble()
        {
            RequireScalar();
            return NimFloatFormatter.Parse(RawText());
        }

        public float GetSingle()
        {
            return (float)GetDouble();
        }

        public void SetSingle(float value)
        {
            RequireScalar();
            if (Type != GffFieldType.Float)
                throw new InvalidOperationException($"Field type {Type} does not hold a float value.");

            EditScope.EnsureMutationAllowed();
            var oldValue = RawValue;
            var oldLocId = RawLocStringId;
            RawValue = Encoding.ASCII.GetBytes(NimFloatFormatter.Format(value));
            EditScope.Capture(new FieldValueEdit(this, oldValue, oldLocId, RawValue, RawLocStringId));
        }

        public void SetDouble(double value)
        {
            RequireScalar();
            if (Type != GffFieldType.Double)
                throw new InvalidOperationException($"Field type {Type} does not hold a double value.");

            EditScope.EnsureMutationAllowed();
            var oldValue = RawValue;
            var oldLocId = RawLocStringId;
            RawValue = Encoding.ASCII.GetBytes(NimFloatFormatter.Format(value));
            EditScope.Capture(new FieldValueEdit(this, oldValue, oldLocId, RawValue, RawLocStringId));
        }

        public uint? GetLocStringId()
        {
            return RawLocStringId == null
                ? null
                : uint.Parse(Encoding.ASCII.GetString(RawLocStringId), CultureInfo.InvariantCulture);
        }

        public uint? GetStructId()
        {
            return RawFieldStructId == null
                ? null
                : uint.Parse(Encoding.ASCII.GetString(RawFieldStructId), CultureInfo.InvariantCulture);
        }

        private string RawText()
        {
            RequireScalar();
            return Encoding.ASCII.GetString(RawValue!);
        }

        private void RequireScalar()
        {
            if (RawValue == null)
                throw new InvalidOperationException($"Field type {Type} has no scalar value.");
        }

        /// <summary>Inserts a struct into this list-typed field's elements at the given index.</summary>
        public void InsertElement(int index, JsonGffStruct element)
        {
            RequireList();
            if (index < 0 || index > Elements!.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            EditScope.EnsureMutationAllowed();
            Elements.Insert(index, element);
            EditScope.Capture(new InsertElementEdit(this, index, element));
        }

        /// <summary>Removes this list-typed field's element at the given index.</summary>
        public void RemoveElementAt(int index)
        {
            RequireList();
            if (index < 0 || index >= Elements!.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            EditScope.EnsureMutationAllowed();
            var element = Elements[index];
            Elements.RemoveAt(index);
            EditScope.Capture(new RemoveElementEdit(this, index, element));
        }

        /// <summary>Moves this list-typed field's element from one index to another.</summary>
        public void MoveElement(int fromIndex, int toIndex)
        {
            RequireList();
            var count = Elements!.Count;
            if (fromIndex < 0 || fromIndex >= count)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= count)
                throw new ArgumentOutOfRangeException(nameof(toIndex));

            if (fromIndex == toIndex)
                return;

            EditScope.EnsureMutationAllowed();
            var element = Elements[fromIndex];
            Elements.RemoveAt(fromIndex);
            Elements.Insert(toIndex, element);
            EditScope.Capture(new MoveElementEdit(this, fromIndex, toIndex));
        }

        private void RequireList()
        {
            if (Type != GffFieldType.List || Elements == null)
                throw new InvalidOperationException($"Field type {Type} is not a list.");
        }

        /// <summary>Inserts a language entry into this cexolocstring field at the given index.</summary>
        public void InsertLocStringEntry(int index, LocStringEntry entry)
        {
            RequireLocString();
            for (var i = 0; i < LocStringEntries!.Count; i++)
            {
                if (LocStringEntries[i].LanguageKey == entry.LanguageKey)
                    throw new ArgumentException($"Language key '{entry.LanguageKey}' already exists.", nameof(entry));
            }

            if (index < 0 || index > LocStringEntries.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            EditScope.EnsureMutationAllowed();
            LocStringEntries.Insert(index, entry);
            EditScope.Capture(new InsertLocStringEntryEdit(this, index, entry));
        }

        /// <summary>Appends a language entry to this cexolocstring field.</summary>
        public void AddLocStringEntry(LocStringEntry entry)
        {
            RequireLocString();
            InsertLocStringEntry(LocStringEntries!.Count, entry);
        }

        /// <summary>Removes this cexolocstring field's entry for the given language key, if present.</summary>
        public bool RemoveLocStringEntry(string languageKey)
        {
            RequireLocString();
            var index = -1;
            for (var i = 0; i < LocStringEntries!.Count; i++)
            {
                if (LocStringEntries[i].LanguageKey != languageKey)
                    continue;

                index = i;
                break;
            }

            if (index < 0)
                return false;

            EditScope.EnsureMutationAllowed();
            var entry = LocStringEntries[index];
            LocStringEntries.RemoveAt(index);
            EditScope.Capture(new RemoveLocStringEntryEdit(this, index, entry));
            return true;
        }

        private void RequireLocString()
        {
            if (Type != GffFieldType.CExoLocString || LocStringEntries == null)
                throw new InvalidOperationException($"Field type {Type} is not a CExoLocString.");
        }
    }
}
