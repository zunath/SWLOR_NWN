using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    [Flags]
    public enum PropertyType
    {
        Invalid = 0,
        Apartment = 1,
        Building = 2,
        Starship = 4,
        City = 8,
        Structure = 16,
        Category = 32
    }
}
