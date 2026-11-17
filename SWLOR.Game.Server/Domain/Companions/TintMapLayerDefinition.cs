namespace SWLOR.Game.Server.Domain.Companions
{
    public sealed record TintMapLayerDefinition(
        string Name,
        string UniformName,
        string ColorUniformName,
        string CustomModeUniformName,
        string PaletteResref,
        int PaletteBaseRow)
    {
        public string ColorRedUniformName => $"{ColorUniformName}R";
        public string ColorGreenUniformName => $"{ColorUniformName}G";
        public string ColorBlueUniformName => $"{ColorUniformName}B";
    }
}
