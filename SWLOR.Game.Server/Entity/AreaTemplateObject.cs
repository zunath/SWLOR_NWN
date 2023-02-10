namespace SWLOR.Game.Server.Entity
{
    public class AreaTemplateObject : EntityBase
    {
        [Indexed]
        public string AreaResref { get; set; }
        [Indexed]
        public string AreaTag { get; set; }
        [Indexed]
        public string ObjectName { get; set; }
        public string ObjectData { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }
        public float LocationZ { get; set; }
        public float LocationOrientation { get; set; }
        public AreaTemplateObject(string areaResRef, string areaTag, string name, string data, string locationX, string locationY, string locationZ, string locationOrientation)
        {
            AreaResref = areaResRef;
            AreaTag = areaTag;
            ObjectName = name;
            ObjectData = data;
        }
        public AreaTemplateObject()
        {
            AreaResref = string.Empty;
            AreaTag = string.Empty;
            ObjectName = string.Empty;
            ObjectData = string.Empty;
        }
    }
}
