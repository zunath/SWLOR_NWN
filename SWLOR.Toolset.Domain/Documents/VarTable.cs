using System.Collections;
using System.Globalization;
using System.Text;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// One row of a "VarTable" list: a local variable's Name (cexostring), Type (dword: 1 = int,
    /// 2 = float, 3 = string, matching NWN's local-variable type convention) and Value (typed
    /// per Type).
    /// </summary>
    public sealed class VarTableEntry
    {
        internal JsonGffStruct Struct { get; }

        internal VarTableEntry(JsonGffStruct target)
        {
            Struct = target;
        }

        public string Name => Struct.GetStringOrNull("Name") ?? string.Empty;

        public int Type => Struct.GetUIntOrNull("Type") is { } value ? (int)value : 0;

        public int? IntValue => Type == VarTable.TypeInt ? Struct.GetIntOrNull("Value") : null;

        public float? FloatValue => Type == VarTable.TypeFloat ? Struct.GetSingleOrNull("Value") : null;

        public string? StringValue => Type == VarTable.TypeString ? Struct.GetStringOrNull("Value") : null;
    }

    /// <summary>
    /// A view over a struct's "VarTable" list field (module/area globals, or a per-instance
    /// local-variable table on a creature/placeable/etc). Reads never allocate the backing list;
    /// writes create the "VarTable" field and/or the named entry on first use.
    /// </summary>
    public sealed class VarTable : IEnumerable<VarTableEntry>
    {
        public const int TypeInt = 1;
        public const int TypeFloat = 2;
        public const int TypeString = 3;

        private const string FieldName = "VarTable";

        private readonly JsonGffStruct _owner;

        public VarTable(JsonGffStruct owner)
        {
            _owner = owner;
        }

        public IEnumerator<VarTableEntry> GetEnumerator()
        {
            foreach (var entryStruct in _owner.GetListOrEmpty(FieldName))
                yield return new VarTableEntry(entryStruct);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int? GetInt(string name)
        {
            var entry = FindByName(name);
            return entry != null && entry.Type == TypeInt ? entry.IntValue : null;
        }

        public float? GetFloat(string name)
        {
            var entry = FindByName(name);
            return entry != null && entry.Type == TypeFloat ? entry.FloatValue : null;
        }

        public string? GetString(string name)
        {
            var entry = FindByName(name);
            return entry != null && entry.Type == TypeString ? entry.StringValue : null;
        }

        public void SetInt(string name, int value)
        {
            SetValue(name, TypeInt, GffFieldType.Int,
                () => Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture)),
                field => field.SetInteger(value));
        }

        public void SetFloat(string name, float value)
        {
            JsonGffField.ValidateFiniteValue(value);
            SetValue(name, TypeFloat, GffFieldType.Float,
                () => Encoding.ASCII.GetBytes(NimFloatFormatter.Format(value)),
                field => field.SetSingle(value));
        }

        public void SetString(string name, string value)
        {
            SetValue(name, TypeString, GffFieldType.CExoString,
                () => JsonStringCodec.Encode(value),
                field => field.SetString(value));
        }

        public bool Remove(string name)
        {
            var listField = _owner.GetOrNull(FieldName);
            if (listField == null)
                return false;
            if (listField.Type != GffFieldType.List || listField.Elements == null)
                throw new InvalidOperationException($"Field '{FieldName}' is not a GFF list.");

            var list = listField.Elements!;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].GetStringOrNull("Name") == name)
                {
                    listField.RemoveElementAt(i);
                    return true;
                }
            }

            return false;
        }

        private VarTableEntry? FindByName(string name)
        {
            foreach (var entryStruct in _owner.GetListOrEmpty(FieldName))
            {
                if (entryStruct.GetStringOrNull("Name") == name)
                    return new VarTableEntry(entryStruct);
            }

            return null;
        }

        private void SetValue(string name, int type, GffFieldType valueType,
            Func<byte[]> encodeInitial, Action<JsonGffField> updateExisting)
        {
            var listField = GetOrAddListField();
            var list = listField.Elements!;
            var insertAt = list.Count;
            for (var index = 0; index < list.Count; index++)
            {
                var entryStruct = list[index];
                if (entryStruct.GetStringOrNull("Name") != name)
                    continue;

                if (entryStruct.GetUIntOrNull("Type") == (uint)type)
                {
                    // Same type: mutate the existing Value field in place for a minimal diff.
                    updateExisting(entryStruct.Get("Value"));
                    return;
                }

                // Type changed: the Value field's GFF type must change too, so replace the entry.
                insertAt = index;
                listField.RemoveElementAt(index);
                break;
            }

            var newEntry = JsonGffField.CreateStruct(0).Struct!;
            newEntry.Add("Name", JsonGffField.CreateScalar(GffFieldType.CExoString, JsonStringCodec.Encode(name)));
            newEntry.Add("Type", JsonGffField.CreateScalar(GffFieldType.Dword,
                Encoding.ASCII.GetBytes(type.ToString(CultureInfo.InvariantCulture))));
            newEntry.Add("Value", JsonGffField.CreateScalar(valueType, encodeInitial()));
            listField.InsertElement(insertAt, newEntry);
        }

        private JsonGffField GetOrAddListField()
        {
            var field = _owner.GetOrNull(FieldName);
            if (field != null)
            {
                if (field.Type != GffFieldType.List || field.Elements == null)
                    throw new InvalidOperationException($"Field '{FieldName}' is not a GFF list.");

                return field;
            }

            field = JsonGffField.CreateList();
            _owner.Add(FieldName, field);
            return field;
        }
    }
}
