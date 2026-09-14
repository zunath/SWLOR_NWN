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

        public float EntryX => Root.GetSingleOrNull("Mod_Entry_X") ?? 0;
        public float EntryY => Root.GetSingleOrNull("Mod_Entry_Y") ?? 0;
        public float EntryZ => Root.GetSingleOrNull("Mod_Entry_Z") ?? 0;

        public LocString Name => Root.GetOrAddLocString("Mod_Name");

        public LocString Description => Root.GetOrAddLocString("Mod_Description");

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

        public string? CustomTlk
        {
            get => Root.GetStringOrNull("Mod_CustomTlk");
            set => Root.SetString("Mod_CustomTlk", GffFieldType.CExoString, value ?? string.Empty);
        }

        public int MinutesPerHour
        {
            get => Root.GetIntOrNull("Mod_MinPerHour") ?? 0;
            set => Root.SetInt("Mod_MinPerHour", GffFieldType.Byte, value);
        }

        public int DawnHour
        {
            get => Root.GetIntOrNull("Mod_DawnHour") ?? 0;
            set => Root.SetInt("Mod_DawnHour", GffFieldType.Byte, value);
        }

        public int DuskHour
        {
            get => Root.GetIntOrNull("Mod_DuskHour") ?? 0;
            set => Root.SetInt("Mod_DuskHour", GffFieldType.Byte, value);
        }

        public int StartingMonth
        {
            get => Root.GetIntOrNull("Mod_StartMonth") ?? 0;
            set => Root.SetInt("Mod_StartMonth", GffFieldType.Byte, value);
        }

        public int StartingDay
        {
            get => Root.GetIntOrNull("Mod_StartDay") ?? 0;
            set => Root.SetInt("Mod_StartDay", GffFieldType.Byte, value);
        }

        public int StartingHour
        {
            get => Root.GetIntOrNull("Mod_StartHour") ?? 0;
            set => Root.SetInt("Mod_StartHour", GffFieldType.Byte, value);
        }

        public uint StartingYear
        {
            get => Root.GetUIntOrNull("Mod_StartYear") ?? 0;
            set => Root.SetUInt("Mod_StartYear", GffFieldType.Dword, value);
        }

        public int XpScale
        {
            get => Root.GetIntOrNull("Mod_XPScale") ?? 0;
            set => Root.SetInt("Mod_XPScale", GffFieldType.Byte, value);
        }

        public string? StartingMovie
        {
            get => Root.GetStringOrNull("Mod_StartMovie");
            set => Root.SetString("Mod_StartMovie", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string GetScript(string fieldName) => Root.GetStringOrNull(fieldName) ?? string.Empty;

        public void SetScript(string fieldName, string value) =>
            Root.SetString(fieldName, GffFieldType.ResRef, value ?? string.Empty);

        public void SetHakNames(IEnumerable<string> names)
        {
            ArgumentNullException.ThrowIfNull(names);
            var list = Root.GetOrAddList("Mod_HakList");
            var field = Root.Get("Mod_HakList");
            while (list.Count > 0)
                field.RemoveElementAt(list.Count - 1);

            foreach (var name in names)
            {
                var entry = JsonGffField.CreateStruct(8).Struct!;
                entry.Add(
                    "Mod_Hak",
                    JsonGffField.CreateScalar(
                        GffFieldType.CExoString,
                        JsonStringCodec.Encode(name)));
                field.InsertElement(field.Elements!.Count, entry);
            }
        }

        public VarTable VarTable => new(Root);
    }
}
