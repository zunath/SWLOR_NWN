namespace SWLOR.Game.Server.Core.NWNX.Enum
{
    public class TilesetData
    {
        public int TileCount { get; set; }
        public float HeightTransition { get; set; }
        public int TerrainCount { get; set; }
        public int CrosserCount { get; set; }
        public int GroupCount { get; set; }
        public string BorderTerrain { get; set; }
        public string DefaultTerrain { get; set; }
        public string FloorTerrain { get; set; }
        public int DisplayNameStringRef { get; set; }
        public string UnlocalizedName { get; set; }
        public bool IsInterior { get; set; }
        public int HasHeightTransition { get; set; }
    }
}
