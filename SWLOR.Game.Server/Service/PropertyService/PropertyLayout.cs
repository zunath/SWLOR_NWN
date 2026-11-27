namespace SWLOR.Game.Server.Service.PropertyService
{
    public class PropertyLayout
    {
        public PropertyType PropertyType { get; set; }
        public string Name { get; set; }
        public int StructureLimit { get; set; }
        public int ItemStorageLimit { get; set; }

        /// <summary>
        /// How many pieces of furniture add item storage, the best first (<see cref="FurnitureStorageRules"/>); 0 means every
        /// piece does. Ships set it below <see cref="StructureLimit"/>, so the extra furniture they hold decorates only.
        /// </summary>
        public int StoragePieceLimit { get; set; }
        public int BuildingLimit { get; set; }
        public int ResearchDeviceLimit { get; set; }
        public int InitialPrice { get; set; }
        public int PricePerDay { get; set; }
        public string AreaInstanceResref { get; set; }
        public bool IsAvailableForRent { get; set; } = true;

        public Action<uint> OnSpawnAction { get; set; }
        public Action<uint, PropertyUpgradeType, int> OnCityUpgradeAction { get; set; }
    }
}
