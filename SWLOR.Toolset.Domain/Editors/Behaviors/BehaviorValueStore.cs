using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>
    /// Reads and writes behavior-owned values on either a blueprint root or an area-instance
    /// struct. Both carry the same fields and VarTable, which lets one behavior editor serve
    /// blueprints and placements alike.
    /// </summary>
    /// <remarks>
    /// Every write here mutates the document directly and must therefore run inside the owning
    /// session's transaction — the view model is what supplies that.
    /// </remarks>
    public class BehaviorValueStore
    {
        private readonly JsonGffStruct _valueStruct;
        private readonly VarTable _locals;

        public BehaviorValueStore(JsonGffStruct valueStruct)
        {
            _valueStruct = valueStruct ?? throw new ArgumentNullException(nameof(valueStruct));
            _locals = new VarTable(valueStruct);
        }

        public JsonGffStruct ValueStruct => _valueStruct;

        public JsonGffStruct Owner => _valueStruct;

        public VarTable Locals => _locals;

        /// <summary>The language-0 text of a CExoLocString field, such as an object's name.</summary>
        public string GetLocalizedText(string name) =>
            _valueStruct.GetLocStringOrNull(name)?.Text ?? string.Empty;

        /// <summary>The field-level TLK reference of a CExoLocString field, if one is present.</summary>
        public uint? GetLocalizedStringRef(string name) =>
            _valueStruct.GetLocStringOrNull(name)?.StrRef;

        public void SetLocalizedText(string name, string value) =>
            _valueStruct.GetOrAddLocString(name).Text = value;

        /// <summary>Whether two complete localized-string fields carry the same TLK reference
        /// and the same ordered language/gender entries, not merely the same English text.</summary>
        public bool LocalizedValuesMatch(string leftName, string rightName)
        {
            var left = _valueStruct.GetOrNull(leftName);
            var right = _valueStruct.GetOrNull(rightName);
            if (left == null || right == null)
                return left == right;
            if (left.Type != GffFieldType.CExoLocString || right.Type != GffFieldType.CExoLocString)
                return false;
            if (!BytesEqual(left.RawLocStringId, right.RawLocStringId))
                return false;

            var leftEntries = left.LocStringEntries ?? new List<LocStringEntry>();
            var rightEntries = right.LocStringEntries ?? new List<LocStringEntry>();
            if (leftEntries.Count != rightEntries.Count)
                return false;

            for (var index = 0; index < leftEntries.Count; index++)
            {
                if (!string.Equals(
                        leftEntries[index].LanguageKey,
                        rightEntries[index].LanguageKey,
                        StringComparison.Ordinal) ||
                    !BytesEqual(leftEntries[index].RawText, rightEntries[index].RawText))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Replaces one localized-string field with an independent complete copy of
        /// another, preserving its TLK reference and every language/gender entry.</summary>
        public void CopyLocalizedValue(string sourceName, string targetName)
        {
            if (_valueStruct.GetOrNull(sourceName) is not { } source)
                throw new KeyNotFoundException($"Localized-string field '{sourceName}' was not found.");
            if (source.Type != GffFieldType.CExoLocString)
                throw new InvalidOperationException($"Field '{sourceName}' is not a localized string.");

            JsonGffField clone;
            using (EditScope.EnterConstruction())
                clone = InstanceFieldMap.CloneField(source);

            _valueStruct.Remove(targetName);
            _valueStruct.Add(targetName, clone);
        }

        public string GetString(BehaviorFieldStorage storage, string name)
        {
            return storage == BehaviorFieldStorage.Local
                ? _locals.GetString(name) ?? string.Empty
                : _valueStruct.GetStringOrNull(name) ?? string.Empty;
        }

        private static bool BytesEqual(byte[]? left, byte[]? right)
        {
            if (left == null || right == null)
                return left == right;

            return left.AsSpan().SequenceEqual(right);
        }

        public void SetString(BehaviorFieldStorage storage, string name, GffFieldType type, string value)
        {
            if (storage == BehaviorFieldStorage.Local)
                _locals.SetString(name, value);
            else
                _valueStruct.SetString(name, type, value);
        }

        public long? GetInteger(BehaviorFieldStorage storage, string name)
        {
            if (storage == BehaviorFieldStorage.Local)
                return _locals.GetInt(name);

            if (!_valueStruct.TryGet(name, out var field))
                return null;

            return field.Type is GffFieldType.Dword or GffFieldType.Dword64
                ? checked((long)field.GetUnsignedInteger())
                : field.GetInteger();
        }

        public void SetInteger(BehaviorFieldStorage storage, string name, GffFieldType type, long value)
        {
            if (storage == BehaviorFieldStorage.Local)
            {
                JsonGffField.ValidateIntegerValue(GffFieldType.Int, value);
                _locals.SetInt(name, checked((int)value));
            }
            else if (type is GffFieldType.Dword or GffFieldType.Dword64)
            {
                JsonGffField.ValidateIntegerValue(type, value);
                _valueStruct.SetULong(name, type, checked((ulong)value));
            }
            else
            {
                JsonGffField.ValidateIntegerValue(type, value);
                _valueStruct.SetLong(name, type, value);
            }
        }

        public double? GetFloat(BehaviorFieldStorage storage, string name)
        {
            return storage == BehaviorFieldStorage.Local
                ? _locals.GetFloat(name)
                : _valueStruct.GetSingleOrNull(name);
        }

        public void SetFloat(BehaviorFieldStorage storage, string name, double value)
        {
            if (storage == BehaviorFieldStorage.Local)
                _locals.SetFloat(name, (float)value);
            else
                _valueStruct.SetSingle(name, (float)value);
        }

        public IReadOnlyList<string> GetResRefList(string listName, string valueName)
        {
            return _valueStruct.GetListOrEmpty(listName)
                .Select(entry => entry.GetStringOrNull(valueName) ?? string.Empty)
                .ToList();
        }

        public void AddResRefListEntry(string listName, string valueName, string resRef)
        {
            JsonGffField.ValidateStringValue(GffFieldType.ResRef, resRef);
            var list = GetOrAddListField(listName);
            var entry = JsonGffField.CreateStruct(0).Struct!;
            entry.SetString(valueName, GffFieldType.ResRef, resRef);
            list.InsertElement(list.Elements!.Count, entry);
        }

        public void RemoveListEntry(string listName, int index)
        {
            var list = _valueStruct.GetOrNull(listName);
            if (list?.Elements == null || index < 0 || index >= list.Elements.Count)
                return;

            list.RemoveElementAt(index);
        }

        public void MoveListEntry(string listName, int fromIndex, int toIndex)
        {
            var list = _valueStruct.GetOrNull(listName);
            if (list?.Elements == null)
                return;

            list.MoveElement(fromIndex, toIndex);
        }

        public void ReplaceResRefList(string listName, string valueName, IEnumerable<string> resRefs)
        {
            var list = GetOrAddListField(listName);
            while (list.Elements!.Count > 0)
                list.RemoveElementAt(list.Elements.Count - 1);

            foreach (var resRef in resRefs)
                AddResRefListEntry(listName, valueName, resRef);
        }

        private JsonGffField GetOrAddListField(string name)
        {
            if (_valueStruct.GetOrNull(name) is { } existing)
                return existing;

            var list = JsonGffField.CreateList();
            _valueStruct.Add(name, list);
            return list;
        }

        /// <summary>Applies one managed value, skipping what only a placement can carry.</summary>
        public void Apply(BehaviorManagedValue value, bool isInstance = true)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.IsInstanceOnly && !isInstance)
                return;

            if (value.StringValue != null)
                SetString(value.Storage, value.Name, value.FieldType, value.StringValue);
            else if (value.IntValue is { } integer)
                SetInteger(value.Storage, value.Name, value.FieldType, integer);
            else if (value.FloatValue is { } number)
                SetFloat(value.Storage, value.Name, number);
        }

        /// <summary>
        /// Whether a managed value currently holds what its behavior says it should — what the
        /// editor's tick beside each managed row means.
        /// </summary>
        public bool Matches(BehaviorManagedValue value, bool isInstance = true)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.IsInstanceOnly && !isInstance)
                return true;

            if (value.StringValue != null)
                return string.Equals(
                    GetString(value.Storage, value.Name), value.StringValue, StringComparison.OrdinalIgnoreCase);

            if (value.IntValue is { } integer)
                return GetInteger(value.Storage, value.Name) == integer;

            if (value.FloatValue is { } number)
                return GetFloat(value.Storage, value.Name) is { } actual && Math.Abs(actual - number) < 1e-4;

            return true;
        }

        /// <summary>
        /// Clears what a behavior owned, so swapping behaviors never leaves the previous one's
        /// scripts firing or its locals lying around.
        /// </summary>
        public void Clear(
            IEnumerable<BehaviorManagedValue> managedValues,
            IEnumerable<BehaviorFieldDefinition> fields)
        {
            ArgumentNullException.ThrowIfNull(managedValues);
            ArgumentNullException.ThrowIfNull(fields);

            foreach (var value in managedValues)
            {
                if (value.ClearOnSwap)
                    ClearOne(value.Storage, value.Name, value.FieldType);
            }

            foreach (var field in fields)
            {
                if (field.Name.Length == 0)
                    continue;

                ClearOne(field.Storage, field.Name, field.FieldType);
            }
        }

        public void ClearOne(BehaviorFieldStorage storage, string name, GffFieldType type)
        {
            if (storage == BehaviorFieldStorage.Local)
            {
                _locals.Remove(name);
                return;
            }

            switch (type)
            {
                case GffFieldType.CExoLocString:
                    SetLocalizedText(name, string.Empty);
                    break;
                case GffFieldType.ResRef:
                case GffFieldType.CExoString:
                    _valueStruct.SetString(name, type, string.Empty);
                    break;
                case GffFieldType.Float:
                    _valueStruct.SetSingle(name, 0f);
                    break;
                case GffFieldType.Dword:
                case GffFieldType.Dword64:
                    _valueStruct.SetUInt(name, type, 0);
                    break;
                case GffFieldType.List:
                    var list = _valueStruct.GetOrNull(name);
                    while (list?.Elements is { Count: > 0 })
                        list.RemoveElementAt(list.Elements.Count - 1);
                    break;
                default:
                    _valueStruct.SetInt(name, type, 0);
                    break;
            }
        }
    }
}
