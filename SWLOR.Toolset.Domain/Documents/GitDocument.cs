using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Typed view over a .git (area instance) nwn_gff JSON document: the per-area lists of
    /// placed object instances, area-level ambient/music properties, and the area's own
    /// local-variable table.
    /// </summary>
    /// <remarks>
    /// Verified list key spellings against the corpus (bank.git.json and others): "Creature
    /// List", "Door List", "List" (loose item instances lying in the area —
    /// NOT a sound list, despite the name), "Placeable List", "SoundList", "StoreList",
    /// "TriggerList", "WaypointList". Spacing is inconsistent between keys (some have a space,
    /// some don't) and must match exactly.
    /// </remarks>
    public sealed class GitDocument : GffDocumentBase
    {
        public GitDocument(JsonGffDocument document) : base(document)
        {
        }

        public static GitDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static GitDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public IReadOnlyList<JsonGffStruct> Creatures => Root.GetListOrEmpty("Creature List");

        public IReadOnlyList<JsonGffStruct> Doors => Root.GetListOrEmpty("Door List");

        /// <summary>Loose item instances placed directly in the area (GFF key: "List").</summary>
        public IReadOnlyList<JsonGffStruct> Items => Root.GetListOrEmpty("List");

        public IReadOnlyList<JsonGffStruct> Placeables => Root.GetListOrEmpty("Placeable List");

        public IReadOnlyList<JsonGffStruct> Sounds => Root.GetListOrEmpty("SoundList");

        public IReadOnlyList<JsonGffStruct> Stores => Root.GetListOrEmpty("StoreList");

        public IReadOnlyList<JsonGffStruct> Triggers => Root.GetListOrEmpty("TriggerList");

        public IReadOnlyList<JsonGffStruct> Waypoints => Root.GetListOrEmpty("WaypointList");

        /// <summary>The area's ambient sound/music struct ("AreaProperties"), if present.</summary>
        public JsonGffStruct? AreaProperties => Root.GetStructOrNull("AreaProperties");

        public VarTable VarTable => new(Root);
    }
}
