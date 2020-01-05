using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum BaseStructureType
    {
        [BaseStructureType("Invalid", false, false)]
        Invalid = 0,
        [BaseStructureType("Control Tower", false, true)]
        ControlTower = 1,
        [BaseStructureType("Drill", false, true)]
        Drill = 2,
        [BaseStructureType("Resource Silo", false, true)]
        ResourceSilo = 3,
        [BaseStructureType("Turret", false, true)]
        Turret = 4,
        [BaseStructureType("Building", false, true)]
        Building = 5,
        [BaseStructureType("Mass Production", false, true)]
        MassProduction = 6,
        [BaseStructureType("Starship Production", false, true)]
        StarshipProduction = 7,
        [BaseStructureType("Furniture", true, false)]
        Furniture = 8,
        [BaseStructureType("Stronidium Silo", false, true)]
        StronidiumSilo = 9,
        [BaseStructureType("Fuel Silo", false, true)]
        FuelSilo = 10,
        [BaseStructureType("Crafting Device", true, false)]
        CraftingDevice = 11,
        [BaseStructureType("Persistent Storage", true, false)]
        PersistentStorage = 12,
        [BaseStructureType("Starship", false, true)]
        Starship = 13
    }

    public class BaseStructureTypeAttribute : Attribute
    {
        public string Name { get; set; }
        public bool CanPlaceInside { get; set; }
        public bool CanPlaceOutside { get; set; }

        public BaseStructureTypeAttribute(string name, bool canPlaceInside, bool canPlaceOutside)
        {
            Name = name;
            CanPlaceInside = canPlaceInside;
            CanPlaceOutside = canPlaceOutside;
        }
    }
}
