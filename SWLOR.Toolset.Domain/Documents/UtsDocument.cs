using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over a .uts (sound blueprint) nwn_gff JSON document.</summary>
    public sealed class UtsDocument : GffDocumentBase
    {
        public UtsDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UtsDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UtsDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

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

        public bool? Active
        {
            get => Root.GetIntOrNull("Active") is { } value ? value != 0 : null;
            set => Root.SetInt("Active", GffFieldType.Byte, value == true ? 1 : 0);
        }

        public int? Volume
        {
            get => Root.GetIntOrNull("Volume");
            set => Root.SetInt("Volume", GffFieldType.Byte, value ?? 0);
        }

        /// <summary>The sound set's resref entries ("Sounds"), each a single-field "Sound" struct.</summary>
        public IReadOnlyList<JsonGffStruct> Sounds => Root.GetListOrEmpty("Sounds");

        /// <summary>Reads the "Sound" resref of one Sounds entry.</summary>
        public static string? GetSoundResRef(JsonGffStruct entry) => entry.GetStringOrNull("Sound");
    }
}
