using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    [Flags]
    public enum PropertyType
    {
        [PropertyType("Invalid", false, PropertyPublicType.Invalid, PropertySpawnType.Invalid)]
        Invalid = 0,
        [PropertyType("Apartment", true, PropertyPublicType.AlwaysPrivate, PropertySpawnType.Instance)]
        Apartment = 1,
        [PropertyType("City Hall", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance)]
        CityHall = 2,
        [PropertyType("Starship", true, PropertyPublicType.AlwaysPrivate, PropertySpawnType.Instance)]
        Starship = 4,
        [PropertyType("City", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Area)]
        City = 8,
        [PropertyType("Structure", false, PropertyPublicType.Invalid, PropertySpawnType.World)]
        Structure = 16,
        [PropertyType("Category", false, PropertyPublicType.Invalid, PropertySpawnType.Invalid)]
        Category = 32,
        [PropertyType("Bank", true, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance)]
        Bank = 64,
        [PropertyType("Medical Center", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance)]
        MedicalCenter = 128,
        [PropertyType("Starport", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance)]
        Starport = 256,
        [PropertyType("Cantina", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance)]
        Cantina = 512,
        [PropertyType("House", true, PropertyPublicType.Adjustable, PropertySpawnType.Instance)]
        House = 1024,
        [PropertyType("Lab", false, PropertyPublicType.Adjustable, PropertySpawnType.Instance)]
        Lab = 2048
    }

    public class PropertyTypeAttribute : Attribute
    {
        public string Name { get; set; }
        public bool HasStorage { get; set; }
        public PropertyPublicType PublicSetting { get; set; }
        public PropertySpawnType SpawnType { get; set; }

        public PropertyTypeAttribute(
            string name, 
            bool hasStorage, 
            PropertyPublicType publicSetting,
            PropertySpawnType spawnType)
        {
            Name = name;
            HasStorage = hasStorage;
            PublicSetting = publicSetting;
            SpawnType = spawnType;
        }
    }
}
