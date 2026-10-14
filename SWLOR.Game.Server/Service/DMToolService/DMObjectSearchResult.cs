namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// One area or object found by the DM chooser.
    /// </summary>
    public class DMObjectSearchResult
    {
        public uint Object { get; }
        public DMObjectCategory Category { get; }
        public string Name { get; }
        public string Tag { get; }
        public string Resref { get; }
        public string AreaName { get; }

        public DMObjectSearchResult(uint obj, DMObjectCategory category, string name, string tag, string resref, string areaName)
        {
            Object = obj;
            Category = category;
            Name = name;
            Tag = tag;
            Resref = resref;
            AreaName = areaName;
        }
    }
}
