using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    [Flags]
    public enum PropertyType
    {
        [PropertyType("Invalid", false, PropertySpawnType.Invalid)]
        Invalid = 0,
        [PropertyType("Apartment", true, PropertySpawnType.Instance)]
        Apartment = 1,
        [PropertyType("City Hall", false, PropertySpawnType.Instance)]
        CityHall = 2,
        [PropertyType("Starship", true, PropertySpawnType.Instance)]
        Starship = 4,
        [PropertyType("City", false, PropertySpawnType.Area)]
        City = 8,
        [PropertyType("Structure", false, PropertySpawnType.World)]
        Structure = 16,
        [PropertyType("Category", false, PropertySpawnType.Invalid)]
        Category = 32,
        [PropertyType("Bank", true, PropertySpawnType.Instance)]
        Bank = 64,
        [PropertyType("Medical Center", false, PropertySpawnType.Instance)]
        MedicalCenter = 128,
        [PropertyType("Starport", false, PropertySpawnType.Instance)]
        Starport = 256,
        [PropertyType("Cantina", false, PropertySpawnType.Instance)]
        Cantina = 512,
        [PropertyType("House", true, PropertySpawnType.Instance)]
        House = 1024,
    }

    public class PropertyTypeAttribute : Attribute
    {
        public string Name { get; set; }
        public bool HasStorage { get; set; }
        public PropertySpawnType SpawnType { get; set; }

        public PropertyTypeAttribute(
            string name, 
            bool hasStorage, 
            PropertySpawnType spawnType)
        {
            Name = name;
            HasStorage = hasStorage;
            SpawnType = spawnType;
        }
    }
}
