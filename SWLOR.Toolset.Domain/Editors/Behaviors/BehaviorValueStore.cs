using SWLOR.Toolset.Domain.Documents;
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

        public void SetLocalizedText(string name, string value) =>
            _valueStruct.GetOrAddLocString(name).Text = value;

        public string GetString(BehaviorFieldStorage storage, string name)
        {
            return storage == BehaviorFieldStorage.Local
                ? _locals.GetString(name) ?? string.Empty
                : _valueStruct.GetStringOrNull(name) ?? string.Empty;
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
                default:
                    _valueStruct.SetInt(name, type, 0);
                    break;
            }
        }
    }
}
