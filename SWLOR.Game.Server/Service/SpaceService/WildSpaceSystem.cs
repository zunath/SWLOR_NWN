namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A Wild Space system: a tier 5.5 area reached only through rifts (plan section 23).</summary>
    public class WildSpaceSystem
    {
        public string AreaResref { get; init; }
        public string Name { get; init; }
        public WildSpaceEnvironment Environment { get; init; }

        /// <summary>The system's own creature table: the Rift Raiders and its ace.</summary>
        public string SpawnTable { get; init; }
    }
}
