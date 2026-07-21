using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Shared JSON (de)serialization for the --areas-file batch format, so the ContentBuilder writer
    /// and the ProcgenReview reader can never drift on serializer options (e.g. one adding an enum
    /// converter the other lacks, silently breaking the round trip).
    /// </summary>
    public static class AreaBatchFile
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string Serialize(IReadOnlyList<AreaBatchFileEntry> entries)
        {
            return JsonSerializer.Serialize(entries, Options);
        }

        public static List<AreaBatchFileEntry> Deserialize(string json)
        {
            return JsonSerializer.Deserialize<List<AreaBatchFileEntry>>(json, Options) ?? new List<AreaBatchFileEntry>();
        }
    }
}
