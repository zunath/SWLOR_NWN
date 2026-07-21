namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    using SWLOR.NWN.API.NWScript.Enum.Item;

    public class TintMapMaterialSelection
    {
        public string ModelResref { get; }
        public TintMapMaterialDefinition Material { get; }
        public uint PaletteSource { get; }
        public bool UsesItemColors { get; }
        public AppearanceArmor ArmorPart { get; }

        public TintMapMaterialSelection(
            string modelResref,
            TintMapMaterialDefinition material,
            uint paletteSource,
            bool usesItemColors,
            AppearanceArmor armorPart)
        {
            ModelResref = modelResref;
            Material = material;
            PaletteSource = paletteSource;
            UsesItemColors = usesItemColors;
            ArmorPart = armorPart;
        }
    }
}
