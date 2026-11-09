using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The area cache and the players in each area (the static class <c>Area</c> until P2-ZL).</summary>
        public static IAreaService Area => FacadeAccessor.Resolve<IAreaService>();

        /// <summary>Fishing rods, bait, fish tables and the cast-and-reel flow (the static class <c>Fishing</c> until P2-ZL).</summary>
        public static IFishingService Fishing => FacadeAccessor.Resolve<IFishingService>();

        /// <summary>Starting, settling and abandoning a fishing fight (the static class <c>FishingFights</c> until P2-ZL).</summary>
        public static IFishingFightsService FishingFights => FacadeAccessor.Resolve<IFishingFightsService>();

        /// <summary>HoloCom calls and their holograms (the static class <c>HoloCom</c> until P2-ZL).</summary>
        public static IHoloComService HoloCom => FacadeAccessor.Resolve<IHoloComService>();

        /// <summary>The module's music, songs and battle themes (the static class <c>Music</c> until P2-ZL).</summary>
        public static IMusicService Music => FacadeAccessor.Resolve<IMusicService>();

        /// <summary>Player notes limits and categories (the static class <c>Notes</c> until P2-ZL).</summary>
        public static INotesService Notes => FacadeAccessor.Resolve<INotesService>();

        /// <summary>The planets and the planet each area belongs to (the static class <c>Planet</c> until P2-ZL).</summary>
        public static IPlanetService Planet => FacadeAccessor.Resolve<IPlanetService>();

        /// <summary>The property system: cities, apartments, structures (the static class <c>Property</c> until P2-ZL).</summary>
        public static IPropertyService Property => FacadeAccessor.Resolve<IPropertyService>();

        /// <summary>The scheduled passenger shuttles (the static class <c>Shuttle</c> until P2-ZL).</summary>
        public static IShuttleService Shuttle => FacadeAccessor.Resolve<IShuttleService>();

        /// <summary>Taxi destinations and the ones a player has registered (the facade <c>Service.Taxi</c> until P2-ZA).</summary>
        public static ITaxiService Taxi => FacadeAccessor.Resolve<ITaxiService>();

        /// <summary>The player-placed traps (the static class <c>Traps</c> until P2-ZL).</summary>
        public static ITrapsService Traps => FacadeAccessor.Resolve<ITrapsService>();

        /// <summary>The area weather (the static class <c>Weather</c> until P2-ZL).</summary>
        public static IWeatherService Weather => FacadeAccessor.Resolve<IWeatherService>();
    }
}
