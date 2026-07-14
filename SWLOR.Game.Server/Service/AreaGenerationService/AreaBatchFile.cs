using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// One area entry in the "--areas-file" JSON contract: SWLOR.ContentBuilder writes this and
    /// SWLOR.ProcgenReview reads it. Parameters carries the full EFFECTIVE MacroLayoutParameters --
    /// post DungeonComposition.BuildLayoutParameters, post Content Builder's Advanced-knob overrides
    /// -- so the review module reproduces exactly what Content Builder previewed, byte for byte.
    /// ThemeKey/TilesetKey/LayoutKey are only needed to resolve which tileset .set/placeholder/
    /// lighting to realize the area against (mirroring the "theme:tileset:layout" resolution the
    /// "--areas" string spec already uses); TilesetKey/LayoutKey empty means "use the theme's own
    /// default profile". Resref is optional (auto-generated when blank). Size is a single square
    /// dimension, matching BatchItem's existing square-area convention for review-module builds.
    /// </summary>
    public sealed class AreaBatchFileEntry
    {
        public string Resref { get; set; } = string.Empty;
        public string ThemeKey { get; set; } = string.Empty;
        public string TilesetKey { get; set; } = string.Empty;
        public string LayoutKey { get; set; } = string.Empty;
        public int Seed { get; set; }
        public int Size { get; set; }

        /// <summary>Mirrors AreaGenerationRequest.EnableDecorations/DecorationDensityPercent. Both are
        /// OPTIONAL with these same defaults, so an older --areas-file entry (or a v1 Content Builder
        /// project saved before decorations existed) without them still deserializes correctly.</summary>
        public bool EnableDecorations { get; set; } = true;
        public int DecorationDensityPercent { get; set; } = 100;

        public MacroLayoutParameters Parameters { get; set; } = new();
    }

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
