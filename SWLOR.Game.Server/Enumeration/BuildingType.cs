using System.ComponentModel;

namespace SWLOR.Game.Server.Enumeration
{
    public enum BuildingType
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Exterior")]
        Exterior = 1,
        [Description("Interior")]
        Interior = 2,
        [Description("Apartment")]
        Apartment = 3,
        [Description("Starship")]
        Starship = 4
    }
}
