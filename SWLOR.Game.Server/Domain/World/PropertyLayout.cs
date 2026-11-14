namespace SWLOR.Game.Server.Domain.World
{
    public class PropertyLayout
    {
        public PropertyType PropertyType { get; set; }
        public string Name { get; set; }
        public int StructureLimit { get; set; }
        public int ItemStorageLimit { get; set; }
        public int BuildingLimit { get; set; }
        public int ResearchDeviceLimit { get; set; }
        public int InitialPrice { get; set; }
        public int PricePerDay { get; set; }
        public string AreaInstanceResref { get; set; }
        public bool IsAvailableForRent { get; set; } = true;

        public Action<uint> OnSpawnAction { get; set; }

        /// <summary>
        /// Run on a hot reload for each live instance of the layout, in place of <see cref="OnSpawnAction"/>: puts back what the
        /// spawn action keeps outside the world (a starport's dock), reading the world and writing nothing. Null when the spawn
        /// action leaves nothing outside the world.
        /// </summary>
        public Action<uint> OnReloadAdoptAction { get; set; }
        public Action<uint, PropertyUpgradeType, int> OnCityUpgradeAction { get; set; }
    }
}
