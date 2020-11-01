using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ValueObject
{
    public class MapPin
    {
        public int PlayerIndex { get; set; }
        public string Text { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public uint Area { get; set; }
        public string Tag { get; set; }
        public NWPlayer Player { get; set; }
        public int Index { get; set; }
    }
}
