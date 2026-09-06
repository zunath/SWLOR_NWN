using System.Globalization;
using System.Text;
using SWLOR.Toolset.Domain.Editing;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// An ordered collection of named GFF fields — the shape of the document root, of a
    /// struct-typed field's value object, and of every list element. Field order is preserved
    /// exactly as parsed; new fields are inserted at nwn_gff's sort position (case-insensitive
    /// ASCII, matching Nim's cmpIgnoreCase) so a saved file matches what the packer would emit.
    /// </summary>
    public sealed class JsonGffStruct
    {
        private readonly List<KeyValuePair<string, JsonGffField>> _entries = new();
        private readonly Dictionary<string, int> _indexByName = new(StringComparer.Ordinal);
        private readonly Dictionary<string, object> _fieldMutationTargets = new(StringComparer.Ordinal);

        /// <summary>Raw number token of this struct's "__struct_id" member, if present.</summary>
        public byte[]? RawStructId { get; internal set; }

        public int Count => _entries.Count;

        /// <summary>This struct's "__struct_id" as a number, or null when the source omitted it.</summary>
        public uint? StructId => RawStructId == null
            ? null
            : uint.Parse(Encoding.ASCII.GetString(RawStructId), NumberStyles.Integer, CultureInfo.InvariantCulture);

        /// <summary>
        /// Rewrites this struct's "__struct_id". Needed because every list in the corpus numbers its
        /// elements by position, so removing one from the middle renumbers the elements after it.
        /// </summary>
        public void SetStructId(uint value)
        {
            EditScope.EnsureMutationAllowed();
            var old = RawStructId;
            RawStructId = Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
            EditScope.Capture(new StructIdEdit(this, old, RawStructId));
        }

        public IReadOnlyList<KeyValuePair<string, JsonGffField>> Entries => _entries;

        public bool TryGet(string name, out JsonGffField field)
        {
            if (_indexByName.TryGetValue(name, out var index))
            {
                field = _entries[index].Value;
                return true;
            }

            field = null!;
            return false;
        }

        public JsonGffField? GetOrNull(string name)
        {
            return TryGet(name, out var field) ? field : null;
        }

        public JsonGffField Get(string name)
        {
            if (!TryGet(name, out var field))
                throw new KeyNotFoundException($"Field '{name}' not found in struct.");

            return field;
        }

        public bool Contains(string name)
        {
            return _indexByName.ContainsKey(name);
        }

        internal object GetFieldMutationTarget(string name)
        {
            if (_fieldMutationTargets.TryGetValue(name, out var target))
                return target;

            target = new object();
            _fieldMutationTargets[name] = target;
            return target;
        }

        /// <summary>Appends a parsed field, preserving document order. Used by the reader.</summary>
        internal void AppendParsed(string name, JsonGffField field)
        {
            if (_indexByName.ContainsKey(name))
                throw new FormatException($"Duplicate field '{name}' in struct.");

            _indexByName[name] = _entries.Count;
            _entries.Add(new KeyValuePair<string, JsonGffField>(name, field));
        }

        /// <summary>
        /// Replaces parsed state while preserving this root object's identity. Editor value stores
        /// retain the root struct rather than the containing document, so assigning a new Root on
        /// reload left every open field bound to the abandoned generation.
        /// </summary>
        internal void ReplaceParsedWith(JsonGffStruct replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            RawStructId = replacement.RawStructId?.ToArray();
            _entries.Clear();
            _indexByName.Clear();
            foreach (var (name, field) in replacement._entries)
                AppendParsed(name, field);
        }

        /// <summary>Adds a new field at nwn_gff's sorted position.</summary>
        public void Add(string name, JsonGffField field)
        {
            if (_indexByName.ContainsKey(name))
                throw new ArgumentException($"Field '{name}' already exists in struct.", nameof(name));

            EditScope.EnsureMutationAllowed();

            var insertAt = _entries.Count;
            for (var i = 0; i < _entries.Count; i++)
            {
                if (CompareIgnoreCase(name, _entries[i].Key) < 0)
                {
                    insertAt = i;
                    break;
                }
            }

            _entries.Insert(insertAt, new KeyValuePair<string, JsonGffField>(name, field));
            ReindexFrom(insertAt);

            EditScope.Capture(new AddFieldEdit(this, name, field));
        }

        public bool Remove(string name)
        {
            if (!_indexByName.TryGetValue(name, out var index))
                return false;

            EditScope.EnsureMutationAllowed();

            var field = _entries[index].Value;
            _entries.RemoveAt(index);
            _indexByName.Remove(name);
            ReindexFrom(index);

            EditScope.Capture(new RemoveFieldEdit(this, name, field));
            return true;
        }

        private void ReindexFrom(int start)
        {
            for (var i = start; i < _entries.Count; i++)
                _indexByName[_entries[i].Key] = i;
        }

        /// <summary>
        /// Nim cmpIgnoreCase semantics: compare byte-wise after ASCII lowercasing; on a full
        /// prefix match the shorter string sorts first.
        /// </summary>
        internal static int CompareIgnoreCase(string left, string right)
        {
            var length = Math.Min(left.Length, right.Length);
            for (var i = 0; i < length; i++)
            {
                var a = ToLowerAscii(left[i]);
                var b = ToLowerAscii(right[i]);
                if (a != b)
                    return a - b;
            }

            return left.Length - right.Length;
        }

        private static char ToLowerAscii(char c)
        {
            return c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
        }
    }
}
