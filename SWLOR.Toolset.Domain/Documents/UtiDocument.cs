using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over a .uti (item blueprint) nwn_gff JSON document.</summary>
    public sealed class UtiDocument : GffDocumentBase
    {
        public UtiDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UtiDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UtiDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public string? TemplateResRef
        {
            get => Root.GetStringOrNull("TemplateResRef");
            set => Root.SetString("TemplateResRef", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? Tag
        {
            get => Root.GetStringOrNull("Tag");
            set => Root.SetString("Tag", GffFieldType.CExoString, value ?? string.Empty);
        }

        public LocString LocalizedName => Root.GetOrAddLocString("LocalizedName");

        public int? BaseItem
        {
            get => Root.GetIntOrNull("BaseItem");
            set => Root.SetInt("BaseItem", GffFieldType.Int, value ?? 0);
        }

        public int? StackSize
        {
            get => Root.GetIntOrNull("StackSize");
            set => Root.SetInt("StackSize", GffFieldType.Word, value ?? 0);
        }

        public uint? Cost
        {
            get => Root.GetUIntOrNull("Cost");
            set => Root.SetUInt("Cost", GffFieldType.Dword, value ?? 0);
        }

        public uint? AddCost
        {
            get => Root.GetUIntOrNull("AddCost");
            set => Root.SetUInt("AddCost", GffFieldType.Dword, value ?? 0);
        }

        /// <summary>The item's property list ("PropertiesList"), each entry an itemproperty struct.</summary>
        public IReadOnlyList<JsonGffStruct> PropertiesList => Root.GetListOrEmpty("PropertiesList");
    }
}
