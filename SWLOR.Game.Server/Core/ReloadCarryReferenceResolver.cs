using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Serialization;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Keeps object identity across all the carried fields of one class (<see cref="ReloadStateCarrier"/>): an object two fields or
    /// two collections share (a status effect in both the creature's set of effects and its set of ticking effects) is written once
    /// and read back as one object. One instance serves one class's write or read.
    /// </summary>
    public sealed class ReloadCarryReferenceResolver : IReferenceResolver
    {
        private readonly Dictionary<object, string> _ids = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, object> _objects = new(StringComparer.Ordinal);

        public object ResolveReference(object context, string reference)
        {
            return _objects.TryGetValue(reference, out var value) ? value : null;
        }

        public string GetReference(object context, object value)
        {
            if (!_ids.TryGetValue(value, out var id))
            {
                id = (_ids.Count + 1).ToString(CultureInfo.InvariantCulture);
                _ids[value] = id;
            }

            return id;
        }

        public bool IsReferenced(object context, object value)
        {
            return _ids.ContainsKey(value);
        }

        public void AddReference(object context, string reference, object value)
        {
            _objects[reference] = value;
            _ids[value] = reference;
        }
    }
}
