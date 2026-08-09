namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    using SWLOR.NWN.API.NWScript.Enum.Item;

    public class TintMapMaterialSelection
    {
        public string ModelResref { get; }
        public TintMapMaterialDefinition Material { get; }
        public uint PaletteSource { get; }
        public uint CreaturePaletteSource { get; }
        public bool UsesItemColors { get; }
        public AppearanceArmor ArmorPart { get; }

        public TintMapMaterialSelection(
            string modelResref,
            TintMapMaterialDefinition material,
            uint paletteSource,
            uint creaturePaletteSource,
            bool usesItemColors,
            AppearanceArmor armorPart)
        {
            ModelResref = modelResref;
            Material = material;
            PaletteSource = paletteSource;
            CreaturePaletteSource = creaturePaletteSource;
            UsesItemColors = usesItemColors;
            ArmorPart = armorPart;
        }

        /// <summary>
        /// Equipment owns its six material dye layers, while skin, hair and tattoo layers always
        /// describe the creature even when the pixels happen to live on an equipped body mesh.
        /// </summary>
        public uint GetPaletteSource(TintMapLayerType layer)
        {
            return TintMapVariable.IsCreatureColorLayer(layer)
                ? CreaturePaletteSource
                : PaletteSource;
        }

        public bool UsesItemColor(TintMapLayerType layer)
        {
            return UsesItemColors && !TintMapVariable.IsCreatureColorLayer(layer);
        }
    }
}
