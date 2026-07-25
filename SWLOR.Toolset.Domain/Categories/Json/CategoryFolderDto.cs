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
    }
}
