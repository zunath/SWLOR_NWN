using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over a .utw (waypoint blueprint) nwn_gff JSON document.</summary>
    public sealed class UtwDocument : GffDocumentBase
    {
        public UtwDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UtwDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UtwDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public string? Tag
        {
            get => Root.GetStringOrNull("Tag");
            set => Root.SetString("Tag", GffFieldType.CExoString, value ?? string.Empty);
        }

        public LocString LocalizedName => Root.GetOrAddLocString("LocalizedName");

        public string? TemplateResRef
        {
            get => Root.GetStringOrNull("TemplateResRef");
            set => Root.SetString("TemplateResRef", GffFieldType.ResRef, value ?? string.Empty);
        }

        public VarTable VarTable => new(Root);
    }
}
