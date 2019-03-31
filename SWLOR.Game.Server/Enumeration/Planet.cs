namespace SWLOR.Game.Server.Enumeration
{
    // Note - these MUST be assigned with power-of-2 values to allow them to be
    // used as bitwise flags in starcharts.
    // 
    // To set up a new planet:
    // - Add its constant here as the next power of two.
    // - Add settings in WeatherService.cs for the planet name to tailor its climate.
    // - Update SpaceService.cs to look for the planet in its list of hyperspace destinations (and handle name <> enum conversions)
    // - Add areas named according to the format "PlanetName - AreaName".  The planet name should be everything up to the first
    //   hyphen, except the trailing space. 
    public enum Planet
    {
        Viscara = 1,
        Tatooine = 2,
        MonCala = 4
    }
}
