namespace SWLOR.Game.Server.Entity
{
    public class AreaTemplate : EntityBase
    {
        [Indexed]
        public string AreaResref { get; set; }
        [Indexed]
        public string ObjectName { get; set; }
        public string ObjectData { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }
        public float LocationZ { get; set; }
        public float LocationOrientation { get; set; }
        public AreaTemplate(string areaResRef, string name, string data, string locationX, string locationY, string locationZ, string locationOrientation)
        {
            AreaResref = areaResRef;
            ObjectName = name;
            ObjectData = data;
        }
        public AreaTemplate()
        {
            AreaResref = string.Empty;
            ObjectName = string.Empty;
            ObjectData = string.Empty;

        }
    }
}
