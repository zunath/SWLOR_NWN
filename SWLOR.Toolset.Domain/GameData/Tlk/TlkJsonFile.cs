using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.Toolset.Domain.GameData.Tlk
{
    /// <summary>
    /// Parses SWLOR's custom TLK file, SWLOR_Haks/sw_tlk/sw_tlk.tlk.json - a plain UTF-8 JSON
    /// document (not nwn_gff), shaped as:
    ///   { "language": 0, "entries": [ { "id": 0, "text": "..." }, ... ] }
    /// Entry ids are sparse (the corpus ranges from 0 to well over 190000 with far fewer entries
    /// than the range implies), so entries are keyed in a dictionary rather than stored
    /// positionally by index.
    /// </summary>
    public sealed class TlkJsonFile
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IReadOnlyDictionary<int, string> _textsById;

        private TlkJsonFile(int language, IReadOnlyDictionary<int, string> textsById)
        {
            Language = language;
            _textsById = textsById;
        }

        public int Language { get; }

        /// <summary>
        /// Number of entries present in the file (not the max entry id).
        /// </summary>
        public int Count => _textsById.Count;

        public static TlkJsonFile Load(string path)
        {
            using var stream = File.OpenRead(path);
            return Parse(stream);
        }

        public static TlkJsonFile Parse(Stream stream)
        {
            var document = JsonSerializer.Deserialize<TlkJsonDocument>(stream, JsonOptions)
                ?? throw new InvalidDataException("sw_tlk.tlk.json is empty or malformed.");

            var textsById = new Dictionary<int, string>();
            foreach (var entry in document.Entries ?? Enumerable.Empty<TlkJsonEntry>())
            {
                textsById[entry.Id] = entry.Text ?? string.Empty;
            }

            return new TlkJsonFile(document.Language, textsById);
        }

        public static TlkJsonFile Parse(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return Parse(stream);
        }

        /// <summary>
        /// Returns the text for the given entry id, or null if the id is not present in the file.
        /// </summary>
        public string? GetText(int entryId)
        {
            return _textsById.TryGetValue(entryId, out var text) ? text : null;
        }

        private sealed class TlkJsonDocument
        {
            [JsonPropertyName("language")]
            public int Language { get; set; }

            [JsonPropertyName("entries")]
            public List<TlkJsonEntry>? Entries { get; set; }
        }

        private sealed class TlkJsonEntry
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
