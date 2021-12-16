using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    [Flags]
    public enum PropertyType
    {
        [PropertyType("Invalid", false, false, PropertySpawnType.Invalid)]
        Invalid = 0,
        [PropertyType("Apartment", true, false, PropertySpawnType.Instance)]
        Apartment = 1,
        [PropertyType("City Hall", false, true, PropertySpawnType.Instance)]
        CityHall = 2,
        [PropertyType("Starship", true, false, PropertySpawnType.Instance)]
        Starship = 4,
        [PropertyType("City", false, true, PropertySpawnType.Area)]
        City = 8,
        [PropertyType("Structure", false, false, PropertySpawnType.World)]
        Structure = 16,
        [PropertyType("Category", false, false, PropertySpawnType.Invalid)]
        Category = 32,
        [PropertyType("Bank", true, true, PropertySpawnType.Instance)]
        Bank = 64,
        [PropertyType("Medical Center", false, true, PropertySpawnType.Instance)]
        MedicalCenter = 128,
        [PropertyType("Starport", false, true, PropertySpawnType.Instance)]
        Starport = 256,
        [PropertyType("Cantina", false, true, PropertySpawnType.Instance)]
        Cantina = 512,
        [PropertyType("House", true, false, PropertySpawnType.Instance)]
        House = 1024,
    }

    public class PropertyTypeAttribute : Attribute
    {
        public string Name { get; set; }
        public bool HasStorage { get; set; }
        public bool IsAlwaysPublic { get; set; }
        public PropertySpawnType SpawnType { get; set; }

        public PropertyTypeAttribute(
            string name, 
            bool hasStorage, 
            bool isAlwaysPublic,
            PropertySpawnType spawnType)
        {
            Name = name;
            HasStorage = hasStorage;
            IsAlwaysPublic = isAlwaysPublic;
            SpawnType = spawnType;
        }
    }
}
