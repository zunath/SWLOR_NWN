using System.Text.Json.Serialization;

namespace SWLOR.Toolset.Domain.Categories.Json
{
    /// <summary>
    /// Wire shape of the whole sidecar. Sections are keyed by the resource extension ("area", "utp",
    /// "utc") rather than by enum name, so the file stays readable to anyone who knows NWN and survives
    /// the enum being reordered.
    /// </summary>
    internal sealed class CategoryFileDto
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("sections")]
        public Dictionary<string, CategorySectionDto>? Sections { get; set; }
    }
}
