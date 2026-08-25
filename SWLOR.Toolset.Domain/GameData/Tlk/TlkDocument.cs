using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SWLOR.NWN.Formats.Tlk;

namespace SWLOR.Toolset.Domain.GameData.Tlk
{
    /// <summary>One explicitly populated row in SWLOR's sparse custom TLK source.</summary>
    public readonly record struct TlkEntry(int Id, string Text);

    /// <summary>
    /// Editable, text-only representation of <c>SWLOR_Haks/sw_tlk/sw_tlk.tlk.json</c>.
    /// Rows are stored sparsely and serialized in ascending id order so editing one row does not
    /// renumber any other custom StrRef.
    /// </summary>
    public sealed class TlkDocument
    {
        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        private readonly SortedDictionary<int, string> _entries;
        private TlkEntry[]? _entrySnapshot;

        private TlkDocument(int language, SortedDictionary<int, string> entries)
        {
            Language = language;
            _entries = entries;
        }

        public int Language { get; }

        public int Count => _entries.Count;

        /// <summary>The greatest populated row id, or -1 when the document has no entries.</summary>
        public int MaxEntryId => _entries.Count == 0 ? -1 : _entries.Last().Key;

        /// <summary>A stable, id-sorted snapshot of the populated rows.</summary>
        public IReadOnlyList<TlkEntry> Entries =>
            _entrySnapshot ??= _entries.Select(pair => new TlkEntry(pair.Key, pair.Value)).ToArray();

        public static TlkDocument Load(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            using var stream = File.OpenRead(path);
            return Parse(stream);
        }

        public static TlkDocument Parse(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            TlkJsonDocument source;
            try
            {
                source = JsonSerializer.Deserialize<TlkJsonDocument>(stream, ReadOptions)
                    ?? throw new InvalidDataException("sw_tlk.tlk.json is empty or malformed.");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("sw_tlk.tlk.json is malformed.", exception);
            }

            if (!source.Language.HasValue || source.Language.Value < 0)
                throw new InvalidDataException("TLK language must be a nonnegative integer.");
            if (source.Entries == null)
                throw new InvalidDataException("TLK entries must be a non-null array.");

            var entries = new SortedDictionary<int, string>();
            var seenIds = new HashSet<int>();
            foreach (var entry in source.Entries)
            {
                if (!entry.Id.HasValue ||
                    entry.Id.Value < 0 ||
                    entry.Id.Value > TlkFormatLimits.MaximumEntryId)
                {
                    throw new InvalidDataException(
                        $"TLK entry id {entry.Id?.ToString() ?? "<missing>"} must be between 0 and " +
                        $"{TlkFormatLimits.MaximumEntryId}.");
                }
                if (!seenIds.Add(entry.Id.Value))
                    throw new InvalidDataException($"TLK entry id {entry.Id.Value} appears more than once.");
                if (entry.Text == null)
                    throw new InvalidDataException($"TLK entry id {entry.Id.Value} must have non-null text.");

                // Empty text is the editor's definition of a blank row. Normalize legacy explicit
                // empty entries to the sparse representation used by SetText/Clear and blank search.
                if (entry.Text.Length > 0)
                    entries.Add(entry.Id.Value, entry.Text);
            }

            return new TlkDocument(source.Language.Value, entries);
        }

        public static TlkDocument Parse(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return Parse(stream);
        }

        public bool ContainsEntry(int entryId) => entryId >= 0 && _entries.ContainsKey(entryId);

        public string? GetText(int entryId) =>
            entryId >= 0 && _entries.TryGetValue(entryId, out var text) ? text : null;

        /// <summary>
        /// Creates or replaces a row without trimming its text. An empty string clears the row;
        /// whitespace-only text is preserved because it may be intentional localized content.
        /// </summary>
        public void SetText(int entryId, string text)
        {
            ValidateEntryId(entryId);
            ArgumentNullException.ThrowIfNull(text);

            if (text.Length == 0)
            {
                Clear(entryId);
                return;
            }

            if (_entries.TryGetValue(entryId, out var previous) && previous == text)
                return;

            _entries[entryId] = text;
            _entrySnapshot = null;
        }

        /// <summary>Removes a populated row without shifting or renumbering any other row.</summary>
        public bool Clear(int entryId)
        {
            ValidateEntryId(entryId);
            var removed = _entries.Remove(entryId);
            if (removed)
                _entrySnapshot = null;
            return removed;
        }

        /// <summary>
        /// Serializes two-space, LF-delimited JSON with rows sorted by id and a final newline.
        /// The relaxed encoder keeps ordinary UTF-8 text readable while still escaping JSON
        /// control characters.
        /// </summary>
        public string ToJson()
        {
            var source = new TlkJsonDocument
            {
                Language = Language,
                Entries = _entries
                    .Select(pair => new TlkJsonEntry { Id = pair.Key, Text = pair.Value })
                    .ToList()
            };

            return JsonSerializer.Serialize(source, WriteOptions)
                       .Replace("\r\n", "\n", StringComparison.Ordinal) +
                   "\n";
        }

        /// <summary>Finds the first absent, unreferenced row beginning at row zero.</summary>
        public int FindFirstAvailableBlank(TlkReferenceIndex references)
        {
            ArgumentNullException.ThrowIfNull(references);
            return FindAvailableInRange(0, MaximumKnownId(references), references) ??
                   FindAvailableAfter(MaximumKnownId(references), references);
        }

        /// <summary>
        /// Finds the next absent, unreferenced row, wrapping to row zero after the greatest known
        /// populated or referenced id. Only when no safe gap exists does it return a new row after
        /// that known range.
        /// </summary>
        public int FindNextAvailableBlank(int currentEntryId, TlkReferenceIndex references)
        {
            ValidateEntryId(currentEntryId);
            ArgumentNullException.ThrowIfNull(references);

            var maximum = MaximumKnownId(references);
            if (currentEntryId < maximum)
            {
                var afterCurrent = FindAvailableInRange(currentEntryId + 1, maximum, references);
                if (afterCurrent.HasValue)
                    return afterCurrent.Value;
            }

            var wrapEnd = Math.Min(currentEntryId - 1, maximum);
            var wrapped = FindAvailableInRange(0, wrapEnd, references);
            return wrapped ?? FindAvailableAfter(maximum, references);
        }

        private int MaximumKnownId(TlkReferenceIndex references) =>
            Math.Max(MaxEntryId, references.MaxReferencedEntryId);

        private int? FindAvailableInRange(int start, int end, TlkReferenceIndex references)
        {
            if (end < start)
                return null;

            for (var id = start; id <= end; id++)
            {
                if (!_entries.ContainsKey(id) && !references.IsReferenced(id))
                    return id;
                if (id == int.MaxValue)
                    break;
            }

            return null;
        }

        private int FindAvailableAfter(int maximum, TlkReferenceIndex references)
        {
            if (maximum >= TlkFormatLimits.MaximumEntryId)
                throw new InvalidOperationException("No additional TLK row ids are available.");

            for (var id = maximum + 1; id <= TlkFormatLimits.MaximumEntryId; id++)
            {
                if (!_entries.ContainsKey(id) && !references.IsReferenced(id))
                    return id;
            }

            throw new InvalidOperationException("No additional TLK row ids are available.");
        }

        private static void ValidateEntryId(int entryId)
        {
            if (entryId < 0 || entryId > TlkFormatLimits.MaximumEntryId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entryId),
                    entryId,
                    $"TLK entry ids must be between 0 and {TlkFormatLimits.MaximumEntryId}.");
            }
        }

        private sealed class TlkJsonDocument
        {
            [JsonPropertyOrder(0)]
            [JsonPropertyName("language")]
            public int? Language { get; set; }

            [JsonPropertyOrder(1)]
            [JsonPropertyName("entries")]
            public List<TlkJsonEntry>? Entries { get; set; }
        }

        private sealed class TlkJsonEntry
        {
            [JsonPropertyOrder(0)]
            [JsonPropertyName("id")]
            public int? Id { get; set; }

            [JsonPropertyOrder(1)]
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
