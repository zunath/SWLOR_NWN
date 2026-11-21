using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The area cache and the players in each area (the static class <c>Area</c> until it was removed).</summary>
        public static IAreaService Area => FacadeAccessor.Resolve<IAreaService>();

        /// <summary>The color doors of the Coxxion Base dungeon (static on the Coxxion terminal's menu until it is converted).</summary>
        public static ICoxxionBaseDoorsService CoxxionBaseDoors => FacadeAccessor.Resolve<ICoxxionBaseDoorsService>();

        /// <summary>Fishing rods, bait, fish tables and the cast-and-reel flow (the static class <c>Fishing</c> until it was removed).</summary>
        public static IFishingService Fishing => FacadeAccessor.Resolve<IFishingService>();

        /// <summary>Starting, settling and abandoning a fishing fight (the static class <c>FishingFights</c> until it was removed).</summary>
        public static IFishingFightsService FishingFights => FacadeAccessor.Resolve<IFishingFightsService>();

        /// <summary>HoloCom calls and their holograms (the static class <c>HoloCom</c> until it was removed).</summary>
        public static IHoloComService HoloCom => FacadeAccessor.Resolve<IHoloComService>();

        /// <summary>The module's music, songs and battle themes (the static class <c>Music</c> until it was removed).</summary>
        public static IMusicService Music => FacadeAccessor.Resolve<IMusicService>();

        /// <summary>Player notes limits and categories (the static class <c>Notes</c> until it was removed).</summary>
        public static INotesService Notes => FacadeAccessor.Resolve<INotesService>();

        /// <summary>The planets and the planet each area belongs to (the static class <c>Planet</c> until it was removed).</summary>
        public static IPlanetService Planet => FacadeAccessor.Resolve<IPlanetService>();

        /// <summary>The property system: cities, apartments, structures (the static class <c>Property</c> until it was removed).</summary>
        public static IPropertyService Property => FacadeAccessor.Resolve<IPropertyService>();

        /// <summary>The property definitions: layouts, structures, permissions and property types.</summary>
        public static IPropertyCatalogService PropertyCatalog => FacadeAccessor.Resolve<IPropertyCatalogService>();

        /// <summary>The boot-time processing of every city and the city level rules.</summary>
        public static IPropertyCityGovernanceService PropertyCityGovernance => FacadeAccessor.Resolve<IPropertyCityGovernanceService>();

        /// <summary>The queue that loads property instances.</summary>
        public static IPropertyLoaderService PropertyLoader => FacadeAccessor.Resolve<IPropertyLoaderService>();

        /// <summary>The registry of property instances and their load states.</summary>
        public static IPropertyLoadRegistryService PropertyLoadRegistry => FacadeAccessor.Resolve<IPropertyLoadRegistryService>();

        /// <summary>The hot reload of the property loader.</summary>
        public static IPropertyLoadReloadService PropertyLoadReload => FacadeAccessor.Resolve<IPropertyLoadReloadService>();

        /// <summary>Spawning a property into the game world.</summary>
        public static IPropertySpawnService PropertySpawn => FacadeAccessor.Resolve<IPropertySpawnService>();

        /// <summary>Property clean-up, deletion and starship dock return.</summary>
        public static IPropertyMaintenanceService PropertyMaintenance => FacadeAccessor.Resolve<IPropertyMaintenanceService>();

        /// <summary>Property permission checks and entering a property.</summary>
        public static IPropertyAccessService PropertyAccess => FacadeAccessor.Resolve<IPropertyAccessService>();

        /// <summary>Placing a structure from its item.</summary>
        public static IPropertyStructurePlacementService PropertyStructurePlacement => FacadeAccessor.Resolve<IPropertyStructurePlacementService>();

        /// <summary>The property terminals and the property menu.</summary>
        public static IPropertyTerminalService PropertyTerminal => FacadeAccessor.Resolve<IPropertyTerminalService>();

        /// <summary>The scheduled passenger shuttles (the static class <c>Shuttle</c> until it was removed).</summary>
        public static IShuttleService Shuttle => FacadeAccessor.Resolve<IShuttleService>();

        /// <summary>The shuttle arrival: delivering a landed flight's passengers and tearing down its instance.</summary>
        public static IShuttleArrivalService ShuttleArrival => FacadeAccessor.Resolve<IShuttleArrivalService>();

        /// <summary>The shuttle boarding calls and boarding of ticket holders.</summary>
        public static IShuttleBoardingService ShuttleBoarding => FacadeAccessor.Resolve<IShuttleBoardingService>();

        /// <summary>The time and scheduler seam of the shuttle services.</summary>
        public static IShuttleClockService ShuttleClock => FacadeAccessor.Resolve<IShuttleClockService>();

        /// <summary>Finding shuttle passengers and placing them at the destination.</summary>
        public static IShuttlePassengerService ShuttlePassenger => FacadeAccessor.Resolve<IShuttlePassengerService>();

        /// <summary>The flight state the shuttle services share.</summary>
        public static IShuttleStateService ShuttleState => FacadeAccessor.Resolve<IShuttleStateService>();

        /// <summary>The shuttle tickets: a player's ride, buying and refunding a ticket, and the next departure.</summary>
        public static IShuttleTicketService ShuttleTicket => FacadeAccessor.Resolve<IShuttleTicketService>();

        /// <summary>The shuttle flights in the air and their interiors.</summary>
        public static IShuttleFlightService ShuttleFlight => FacadeAccessor.Resolve<IShuttleFlightService>();

        /// <summary>Taxi destinations and the ones a player has registered (the facade <c>Service.Taxi</c> until it was removed).</summary>
        public static ITaxiService Taxi => FacadeAccessor.Resolve<ITaxiService>();

        /// <summary>The player-placed traps (the static class <c>Traps</c> until it was removed).</summary>
        public static ITrapsService Traps => FacadeAccessor.Resolve<ITrapsService>();

        /// <summary>The area weather (the static class <c>Weather</c> until it was removed).</summary>
        public static IWeatherService Weather => FacadeAccessor.Resolve<IWeatherService>();
    }
}
