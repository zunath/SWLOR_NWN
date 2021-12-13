using System;

namespace SWLOR.Game.Server.Enumeration
{
    // Note - these MUST be assigned with power-of-2 values to allow them to be
    // used as bitwise flags in starcharts.
    // 
    // To set up a new planet:
    // - Add its constant here as the next power of two. (1, 2, 4, 8, 16, 32, 64, 128, 256, 512, etc)
    // - Add settings in WeatherService.cs for the planet name to tailor its climate.
    // - Add areas named according to the format "PlanetName - AreaName".  The planet name should be everything up to the first
    //   hyphen, except the trailing space. 
    // - Add the planet to the Planet.cs service class.
    [Flags]
    public enum PlanetType
    {
        [Planet("Invalid", "-- Invalid --", "", false)]
        Invalid = 0,
        [Planet("Viscara", "Viscara - ", "Viscara_Orbit", true)]
        Viscara = 1,
        [Planet("Tatooine", "Tatooine - ", "Tatooine_Orbit", true)]
        Tatooine = 2,
        [Planet("Mon Cala", "Mon Cala - ", "MonCala_Orbit", true)]
        MonCala = 4,
        [Planet("Hutlar", "Hutlar - ", "Hutlar_Orbit", true)]
        Hutlar = 8,
        [Planet("CZ-220", "CZ-220 - ", "CZ220_Orbit", true)]
        CZ220 = 16
    }

    public class PlanetAttribute : Attribute
    {
        public string Name { get; set; }
        public string Prefix { get; set; }
        public string SpaceOrbitWaypointTag { get; set; }
        public bool IsActive { get; set; }

        public PlanetAttribute(
            string name, 
            string prefix,
            string spaceOrbitWaypointTag,
            bool isActive)
        {
            Name = name;
            Prefix = prefix;
            SpaceOrbitWaypointTag = spaceOrbitWaypointTag;
            IsActive = isActive;
        }
    }
}
