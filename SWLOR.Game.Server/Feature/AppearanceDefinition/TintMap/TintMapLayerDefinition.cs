namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public class TintMapLayerDefinition
    {
        public string Name { get; }
        public string UniformName { get; }
        public string ColorUniformName { get; }
        public string PaletteResref { get; }
        public int PaletteBaseRow { get; }

        public TintMapLayerDefinition(
            string name,
            string uniformName,
            string colorUniformName,
            string paletteResref,
            int paletteBaseRow)
        {
            Name = name;
            UniformName = uniformName;
            ColorUniformName = colorUniformName;
            PaletteResref = paletteResref;
            PaletteBaseRow = paletteBaseRow;
        }
    }
}
