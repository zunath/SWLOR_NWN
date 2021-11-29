using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    [Flags]
    public enum PropertyType
    {
        [PropertyType("Invalid", false, false)]
        Invalid = 0,
        [PropertyType("Apartment", true, true)]
        Apartment = 1,
        [PropertyType("City Hall", true, true)]
        CityHall = 2,
        [PropertyType("Starship", true, true)]
        Starship = 4,
        [PropertyType("City", false, false)]
        City = 8,
        [PropertyType("Structure", false, true)]
        Structure = 16,
        [PropertyType("Category", false, false)]
        Category = 32,
        [PropertyType("Bank", true, true)]
        Bank = 64,
        [PropertyType("Medical Center", false, true)]
        MedicalCenter = 128,
        [PropertyType("Starport", false, true)]
        Starport = 256,
        [PropertyType("Cantina", false, true)]
        Cantina = 512,
        [PropertyType("House", true, true)]
        House = 1024,
    }

    public class PropertyTypeAttribute : Attribute
    {
        public string Name { get; set; }
        public bool HasStorage { get; set; }
        public bool ExistsInGameWorld { get; set; }

        public PropertyTypeAttribute(
            string name, 
            bool hasStorage, 
            bool existsInGameWorld)
        {
            Name = name;
            HasStorage = hasStorage;
            ExistsInGameWorld = existsInGameWorld;
        }
    }
}
