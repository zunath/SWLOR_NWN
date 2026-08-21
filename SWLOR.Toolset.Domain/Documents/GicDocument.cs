using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

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

        /// <summary>
        /// Inserts an empty comment row at the same index as a newly placed GIT instance.
        /// </summary>
        public void InsertBlankComment(
            string listFieldName,
            ResourceType type,
            int index,
            int expectedCount)
        {
            var list = GetOrCreateList(listFieldName);
            while (list.Elements!.Count < index)
                list.InsertElement(list.Elements.Count, CreateBlankComment(type));

            var entry = CreateBlankComment(type);
            list.InsertElement(index, entry);
            AlignCount(list, type, expectedCount);
        }

        /// <summary>
        /// Inserts a deep copy of an object's paired comment, or a blank row when the source area had
        /// no aligned comment entry.
        /// </summary>
        public void InsertCopiedComment(
            string listFieldName,
            ResourceType type,
            int index,
            int expectedCount,
            JsonGffStruct? source)
        {
            var list = GetOrCreateList(listFieldName);
            while (list.Elements!.Count < index)
                list.InsertElement(list.Elements.Count, CreateBlankComment(type));

            var entry = source != null
                ? InstanceFieldMap.Duplicate(source)
                : CreateBlankComment(type);
            list.InsertElement(index, entry);
            AlignCount(list, type, expectedCount);
        }

        /// <summary>Duplicates the comment row paired with a duplicated GIT instance.</summary>
        public void DuplicateComment(
            string listFieldName,
            ResourceType type,
            int index,
            int expectedCount)
        {
            var list = GetOrCreateList(listFieldName);
            while (list.Elements!.Count <= index)
                list.InsertElement(list.Elements.Count, CreateBlankComment(type));

            list.InsertElement(index + 1, InstanceFieldMap.Duplicate(list.Elements[index]));
            AlignCount(list, type, expectedCount);
        }

        /// <summary>Removes the comment row paired with a deleted GIT instance.</summary>
        public void RemoveComment(
            string listFieldName,
            ResourceType type,
            int index,
            int expectedCount)
        {
            var list = GetOrCreateList(listFieldName);
            if (index >= 0 && index < list.Elements!.Count)
                list.RemoveElementAt(index);

            AlignCount(list, type, expectedCount);
        }

        private JsonGffField GetOrCreateList(string listFieldName)
        {
            var list = Root.GetOrNull(listFieldName);
            if (list != null)
                return list;

            list = JsonGffField.CreateList();
            Root.Add(listFieldName, list);
            return list;
        }

        private static JsonGffStruct CreateBlankComment(ResourceType type)
        {
            var entry = JsonGffField.CreateStruct(CommentStructId(type)).Struct!;
            entry.SetString("Comment", GffFieldType.CExoString, string.Empty);
            return entry;
        }

        private static void AlignCount(JsonGffField list, ResourceType type, int expectedCount)
        {
            while (list.Elements!.Count < expectedCount)
                list.InsertElement(list.Elements.Count, CreateBlankComment(type));
            while (list.Elements.Count > expectedCount)
                list.RemoveElementAt(list.Elements.Count - 1);
        }

        private static uint CommentStructId(ResourceType type) => type switch
        {
            ResourceType.Uti => 0,
            ResourceType.Utt => 1,
            ResourceType.Utc => 4,
            ResourceType.Utw => 5,
            ResourceType.Uts => 6,
            ResourceType.Utd => 8,
            ResourceType.Utp => 9,
            ResourceType.Utm => 11,
            _ => throw new ArgumentOutOfRangeException(
                nameof(type), type, "No GIC comment struct id exists for this resource type.")
        };
    }
}
