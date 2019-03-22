using NWN;

namespace SWLOR.Game.Server.GameObject
{
    public class NWLocation
    {
        public Location Location { get; set; }

        public NWLocation(Location nwnLocation)
        {
            Location = nwnLocation;
        }

        public float X => _.GetPositionFromLocation(Location).m_X;
        public float Y => _.GetPositionFromLocation(Location).m_Y;
        public float Z => _.GetPositionFromLocation(Location).m_Z;
        public float Orientation => _.GetFacingFromLocation(Location);

        public NWArea Area => _.GetAreaFromLocation(Location);

        public Vector Position => _.GetPositionFromLocation(Location);

        public static implicit operator Location(NWLocation l)
        {
            return l.Location;
        }

        public static implicit operator NWLocation(Location l)
        {
            return new NWLocation(l);
        }

    }
}
