using System.Text.Json.Serialization;

namespace SWLOR.Toolset.Domain.Categories.Json
{
    /// <summary>Wire shape of one folder in <c>toolset/categories.json</c>.</summary>
    internal sealed class CategoryFolderDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("children")]
        public List<CategoryFolderDto>? Children { get; set; }

        [JsonPropertyName("members")]
        public List<string>? Members { get; set; }

        /// <summary>
        /// True when the importer wrote this folder's name as an unresolved "Category N" placeholder.
        /// Absent (defaults false) in every sidecar written before this marker existed, which is
        /// deliberate: those files' placeholders stay exactly as named until a builder renames them,
        /// rather than being auto-repaired from name text alone the way a "Category 7" a builder typed
        /// on purpose must never be.
        /// </summary>
        [JsonPropertyName("placeholder")]
        public bool Placeholder { get; set; }
    }
}
