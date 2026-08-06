using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over a .utp (placeable blueprint) nwn_gff JSON document.</summary>
    public sealed class UtpDocument : GffDocumentBase
    {
        public UtpDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UtpDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UtpDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

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

        public string? OnUsed
        {
            get => Root.GetStringOrNull("OnUsed");
            set => Root.SetString("OnUsed", GffFieldType.ResRef, value ?? string.Empty);
        }

        public bool? Useable
        {
            get => Root.GetIntOrNull("Useable") is { } value ? value != 0 : null;
            set => Root.SetInt("Useable", GffFieldType.Byte, value == true ? 1 : 0);
        }

        public bool? Static
        {
            get => Root.GetIntOrNull("Static") is { } value ? value != 0 : null;
            set => Root.SetInt("Static", GffFieldType.Byte, value == true ? 1 : 0);
        }

        public bool? Plot
        {
            get => Root.GetIntOrNull("Plot") is { } value ? value != 0 : null;
            set => Root.SetInt("Plot", GffFieldType.Byte, value == true ? 1 : 0);
        }

        public VarTable VarTable => new(Root);
    }
}
