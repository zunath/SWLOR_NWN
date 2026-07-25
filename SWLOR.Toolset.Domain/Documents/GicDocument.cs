using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Typed view over a .gic (area instance comments) nwn_gff JSON document: the toolset-only
    /// "Comment" text attached to each placed object, keyed by the same per-type lists as the
    /// matching .git file (same key spellings, same struct order/count).
    /// </summary>
    public sealed class GicDocument : GffDocumentBase
    {
        public GicDocument(JsonGffDocument document) : base(document)
        {
        }

        public static GicDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static GicDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public IReadOnlyList<JsonGffStruct> Creatures => Root.GetListOrEmpty("Creature List");

        public IReadOnlyList<JsonGffStruct> Doors => Root.GetListOrEmpty("Door List");

        public IReadOnlyList<JsonGffStruct> Items => Root.GetListOrEmpty("List");

        public IReadOnlyList<JsonGffStruct> Placeables => Root.GetListOrEmpty("Placeable List");

        public IReadOnlyList<JsonGffStruct> Sounds => Root.GetListOrEmpty("SoundList");

        public IReadOnlyList<JsonGffStruct> Stores => Root.GetListOrEmpty("StoreList");

        public IReadOnlyList<JsonGffStruct> Triggers => Root.GetListOrEmpty("TriggerList");

        public IReadOnlyList<JsonGffStruct> Waypoints => Root.GetListOrEmpty("WaypointList");

        /// <summary>Reads the "Comment" field of one comment-list entry.</summary>
        public static string? GetComment(JsonGffStruct entry) => entry.GetStringOrNull("Comment");

        /// <summary>Sets the "Comment" field of one comment-list entry.</summary>
        public static void SetComment(JsonGffStruct entry, string value) =>
            entry.SetString("Comment", GffFieldType.CExoString, value);
    }
}
