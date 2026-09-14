using System.Text.Json.Serialization;

namespace SWLOR.Toolset.Domain.Categories.Json
{
    /// <summary>Wire shape of one resource type's section in <c>toolset/categories.json</c>.</summary>
    internal sealed class CategorySectionDto
    {
        [JsonPropertyName("groupBy")]
        public string? GroupBy { get; set; }

        [JsonPropertyName("pinned")]
        public List<string>? Pinned { get; set; }

        [JsonPropertyName("folders")]
        public List<CategoryFolderDto>? Folders { get; set; }

        [JsonPropertyName("seeded")]
        public bool Seeded { get; set; }
    }
}
