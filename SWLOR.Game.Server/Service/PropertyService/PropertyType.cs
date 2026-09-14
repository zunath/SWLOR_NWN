namespace SWLOR.Game.Server.Service.PropertyService
{
    [Flags]
    public enum PropertyType
    {
        [PropertyType("Invalid", false, PropertyPublicType.Invalid, PropertySpawnType.Invalid, PropertyLoadType.Invalid)]
        Invalid = 0,
        [PropertyType("Apartment", true, PropertyPublicType.AlwaysPrivate, PropertySpawnType.Instance, PropertyLoadType.OnDemand)]
        Apartment = 1,
        [PropertyType("City Hall", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]
        CityHall = 2,
        [PropertyType("Starship", true, PropertyPublicType.AlwaysPrivate, PropertySpawnType.Instance, PropertyLoadType.OnDemand)]
        Starship = 4,
        [PropertyType("City", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Area, PropertyLoadType.Startup)]
        City = 8,
        [PropertyType("Structure", false, PropertyPublicType.Invalid, PropertySpawnType.World, PropertyLoadType.Startup)]
        Structure = 16,
        [PropertyType("Category", false, PropertyPublicType.Invalid, PropertySpawnType.Invalid, PropertyLoadType.Invalid)]
        Category = 32,
        [PropertyType("Bank", true, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]
        Bank = 64,
        [PropertyType("Medical Center", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]
        MedicalCenter = 128,
        [PropertyType("Starport", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]
        Starport = 256,
        [PropertyType("Cantina", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]
        Cantina = 512,
        [PropertyType("House", true, PropertyPublicType.Adjustable, PropertySpawnType.Instance, PropertyLoadType.Startup)]
        House = 1024,
        [PropertyType("Lab", false, PropertyPublicType.Adjustable, PropertySpawnType.Instance, PropertyLoadType.Startup)]
        Lab = 2048
    }

    public class PropertyTypeAttribute : Attribute
    {
        public string Name { get; set; }
        public bool HasStorage { get; set; }
        public PropertyPublicType PublicSetting { get; set; }
        public PropertySpawnType SpawnType { get; set; }
        public PropertyLoadType LoadType { get; set; }

        public PropertyTypeAttribute(
            string name,
            bool hasStorage,
            PropertyPublicType publicSetting,
            PropertySpawnType spawnType,
            PropertyLoadType loadType)
        {
            Name = name;
            HasStorage = hasStorage;
            PublicSetting = publicSetting;
            SpawnType = spawnType;
            LoadType = loadType;
        }
    }
}
