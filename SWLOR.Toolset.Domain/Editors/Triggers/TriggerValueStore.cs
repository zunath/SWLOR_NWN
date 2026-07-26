using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// Reads and writes a trigger's values, whichever of the two shapes it has: a blueprint
    /// document's root, or one entry of an area's TriggerList. Both are a
    /// <see cref="JsonGffStruct"/> carrying the same fields and the same VarTable, which is what
    /// lets one editor serve blueprints and placements alike.
    /// </summary>
    /// <remarks>
    /// Every write here mutates the document directly and must therefore run inside the owning
    /// session's transaction — the view model is what supplies that.
    /// </remarks>
    public sealed class TriggerValueStore
    {
        private readonly JsonGffStruct _trigger;
        private readonly VarTable _locals;

        public TriggerValueStore(JsonGffStruct trigger)
        {
            _trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
            _locals = new VarTable(trigger);
        }

        public JsonGffStruct Trigger => _trigger;

        public VarTable Locals => _locals;

        /// <summary>The language-0 text of a CExoLocString field, such as the trigger's name.</summary>
        public string GetLocalizedText(string name) =>
            _trigger.GetLocStringOrNull(name)?.Text ?? string.Empty;

        public void SetLocalizedText(string name, string value) =>
            _trigger.GetOrAddLocString(name).Text = value;

        public string GetString(TriggerFieldStorage storage, string name)
        {
            return storage == TriggerFieldStorage.Local
                ? _locals.GetString(name) ?? string.Empty
                : _trigger.GetStringOrNull(name) ?? string.Empty;
        }

        public void SetString(TriggerFieldStorage storage, string name, GffFieldType type, string value)
        {
            if (storage == TriggerFieldStorage.Local)
                _locals.SetString(name, value);
            else
                _trigger.SetString(name, type, value);
        }

        public long? GetInteger(TriggerFieldStorage storage, string name)
        {
            return storage == TriggerFieldStorage.Local
                ? _locals.GetInt(name)
                : _trigger.GetIntOrNull(name);
        }

        public void SetInteger(TriggerFieldStorage storage, string name, GffFieldType type, long value)
        {
            if (storage == TriggerFieldStorage.Local)
                _locals.SetInt(name, (int)value);
            else if (type == GffFieldType.Dword || type == GffFieldType.Dword64)
                _trigger.SetUInt(name, type, (uint)value);
            else
                _trigger.SetInt(name, type, (int)value);
        }

        public double? GetFloat(TriggerFieldStorage storage, string name)
        {
            return storage == TriggerFieldStorage.Local
                ? _locals.GetFloat(name)
                : _trigger.GetSingleOrNull(name);
        }

        public void SetFloat(TriggerFieldStorage storage, string name, double value)
        {
            if (storage == TriggerFieldStorage.Local)
                _locals.SetFloat(name, (float)value);
            else
                _trigger.SetSingle(name, (float)value);
        }

        /// <summary>Applies one managed value, skipping what only a placement can carry.</summary>
        public void Apply(TriggerManagedValue value, bool isInstance = true)
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
        public bool Matches(TriggerManagedValue value, bool isInstance = true)
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
        public void Clear(TriggerBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);

            foreach (var value in behavior.Manages)
            {
                if (value.ClearOnSwap)
                    ClearOne(value.Storage, value.Name, value.FieldType);
            }

            foreach (var field in behavior.Fields)
            {
                if (field.Name.Length == 0)
                    continue;

                ClearOne(field.Storage, field.Name, field.FieldType);
            }
        }

        private void ClearOne(TriggerFieldStorage storage, string name, GffFieldType type)
        {
            if (storage == TriggerFieldStorage.Local)
            {
                _locals.Remove(name);
                return;
            }

            switch (type)
            {
                case GffFieldType.ResRef:
                case GffFieldType.CExoString:
                    _trigger.SetString(name, type, string.Empty);
                    break;
                case GffFieldType.Float:
                    _trigger.SetSingle(name, 0f);
                    break;
                case GffFieldType.Dword:
                case GffFieldType.Dword64:
                    _trigger.SetUInt(name, type, 0);
                    break;
                default:
                    _trigger.SetInt(name, type, 0);
                    break;
            }
        }
    }
}
