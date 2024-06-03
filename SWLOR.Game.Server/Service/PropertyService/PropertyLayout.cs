using System;

namespace SWLOR.Game.Server.Service.PropertyService
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

        public Action<uint> OnSpawnAction { get; set; }
        public Action<uint, PropertyUpgradeType, int> OnCityUpgradeAction { get; set; }
    }
}
