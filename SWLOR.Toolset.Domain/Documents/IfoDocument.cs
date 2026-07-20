using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over the module.ifo (module properties) nwn_gff JSON document.</summary>
    public sealed class IfoDocument : GffDocumentBase
    {
        public IfoDocument(JsonGffDocument document) : base(document)
        {
        }

        public static IfoDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static IfoDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        /// <summary>The module's area list ("Mod_Area_list"), each entry an "Area_Name" resref.</summary>
        public IReadOnlyList<JsonGffStruct> AreaList => Root.GetListOrEmpty("Mod_Area_list");

        /// <summary>The module's area resrefs, projected from <see cref="AreaList"/>.</summary>
        public IReadOnlyList<string> AreaResRefs =>
            AreaList.Select(s => s.GetStringOrNull("Area_Name") ?? string.Empty).ToList();

        public string? EntryArea
        {
            get => Root.GetStringOrNull("Mod_Entry_Area");
            set => Root.SetString("Mod_Entry_Area", GffFieldType.ResRef, value ?? string.Empty);
        }

        public LocString Name => Root.GetOrAddLocString("Mod_Name");

        public string? Tag
        {
            get => Root.GetStringOrNull("Mod_Tag");
            set => Root.SetString("Mod_Tag", GffFieldType.CExoString, value ?? string.Empty);
        }

        /// <summary>The module's hak list ("Mod_HakList"), each entry a "Mod_Hak" cexostring.</summary>
        public IReadOnlyList<JsonGffStruct> HakList => Root.GetListOrEmpty("Mod_HakList");

        /// <summary>The module's hak names, projected from <see cref="HakList"/>.</summary>
        public IReadOnlyList<string> HakNames =>
            HakList.Select(s => s.GetStringOrNull("Mod_Hak") ?? string.Empty).ToList();

        public VarTable VarTable => new(Root);
    }
}
