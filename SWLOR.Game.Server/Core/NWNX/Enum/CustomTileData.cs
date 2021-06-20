namespace SWLOR.Game.Server.Core.NWNX.Enum
{
    public class CustomTileData
    {
        public int TileId { get; set; }
        public int Orientation { get; set; }
        public int Height { get; set; }

        public int MainLightColor1 { get; set; }
        public int MainLightColor2 { get; set; }
        public int SourceLightColor1 { get; set; }
        public int SourceLightColor2 { get; set; }

        public bool AnimationLoop1 { get; set; }
        public bool AnimationLoop2 { get; set; }
        public bool AnimationLoop3 { get; set; }
    }
}
