using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Typed view over a .utm (store/merchant blueprint) nwn_gff JSON document.
    /// </summary>
    /// <remarks>
    /// Deviation from the generic blueprint pattern: .utm uses "ResRef" for the template resref,
    /// not "TemplateResRef" as every other blueprint type does (verified against bartender.utm.json).
    /// </remarks>
    public sealed class UtmDocument : GffDocumentBase
    {
        public UtmDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UtmDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UtmDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public string? ResRef
        {
            get => Root.GetStringOrNull("ResRef");
            set => Root.SetString("ResRef", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? Tag
        {
            get => Root.GetStringOrNull("Tag");
            set => Root.SetString("Tag", GffFieldType.CExoString, value ?? string.Empty);
        }

        public LocString LocName => Root.GetOrAddLocString("LocName");

        /// <summary>The store's item lists ("StoreList"), one entry per store page/category.</summary>
        public IReadOnlyList<JsonGffStruct> StoreList => Root.GetListOrEmpty("StoreList");

        public int? MarkUp
        {
            get => Root.GetIntOrNull("MarkUp");
            set => Root.SetInt("MarkUp", GffFieldType.Int, value ?? 0);
        }

        public int? MarkDown
        {
            get => Root.GetIntOrNull("MarkDown");
            set => Root.SetInt("MarkDown", GffFieldType.Int, value ?? 0);
        }

        public string? OnOpenStore
        {
            get => Root.GetStringOrNull("OnOpenStore");
            set => Root.SetString("OnOpenStore", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? OnStoreClosed
        {
            get => Root.GetStringOrNull("OnStoreClosed");
            set => Root.SetString("OnStoreClosed", GffFieldType.ResRef, value ?? string.Empty);
        }
    }
}
