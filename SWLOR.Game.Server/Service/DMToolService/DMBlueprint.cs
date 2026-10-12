namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// A blueprint the DM creator can spawn.
    /// </summary>
    public class DMBlueprint
    {
        public string Resref { get; }
        public string Name { get; }
        public string Tag { get; }
        public DMBlueprintCategory Category { get; }
        public DMBlueprintSource Source { get; }

        public DMBlueprint(string resref, string name, string tag, DMBlueprintCategory category, DMBlueprintSource source)
        {
            Resref = resref;
            Name = name;
            Tag = tag;
            Category = category;
            Source = source;
        }
    }
}
