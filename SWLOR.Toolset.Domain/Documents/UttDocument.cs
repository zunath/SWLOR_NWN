using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over a .utt (trigger blueprint) nwn_gff JSON document.</summary>
    public sealed class UttDocument : GffDocumentBase
    {
        public UttDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UttDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UttDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

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

        public VarTable VarTable => new(Root);
    }
}
