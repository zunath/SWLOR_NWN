using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over a .utd (door blueprint) nwn_gff JSON document.</summary>
    public sealed class UtdDocument : GffDocumentBase
    {
        public UtdDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UtdDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UtdDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

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

        public LocString LocName => Root.GetOrAddLocString("LocName");

        public uint? Appearance
        {
            get => Root.GetUIntOrNull("Appearance");
            set => Root.SetUInt("Appearance", GffFieldType.Dword, value ?? 0);
        }

        public bool? Locked
        {
            get => Root.GetIntOrNull("Locked") is { } value ? value != 0 : null;
            set => Root.SetInt("Locked", GffFieldType.Byte, value == true ? 1 : 0);
        }

        public VarTable VarTable => new(Root);
    }
}
