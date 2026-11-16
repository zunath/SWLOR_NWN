using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The area cache and the players in each area (the static class <c>Area</c> until P2-ZL).</summary>
        public static IAreaService Area => FacadeAccessor.Resolve<IAreaService>();

        /// <summary>The color doors of the Coxxion Base dungeon (static on the Coxxion terminal's menu until P5-13).</summary>
        public static ICoxxionBaseDoorsService CoxxionBaseDoors => FacadeAccessor.Resolve<ICoxxionBaseDoorsService>();

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

        /// <summary>The property definitions: layouts, structures, permissions and property types (P5-9b).</summary>
        public static IPropertyCatalogService PropertyCatalog => FacadeAccessor.Resolve<IPropertyCatalogService>();

        /// <summary>The boot-time processing of every city and the city level rules (P5-9b).</summary>
        public static IPropertyCityGovernanceService PropertyCityGovernance => FacadeAccessor.Resolve<IPropertyCityGovernanceService>();

        /// <summary>The queue that loads property instances (P5-9b).</summary>
        public static IPropertyLoaderService PropertyLoader => FacadeAccessor.Resolve<IPropertyLoaderService>();

        /// <summary>The registry of property instances and their load states (P5-9b).</summary>
        public static IPropertyLoadRegistryService PropertyLoadRegistry => FacadeAccessor.Resolve<IPropertyLoadRegistryService>();

        /// <summary>The hot reload of the property loader (P5-9b).</summary>
        public static IPropertyLoadReloadService PropertyLoadReload => FacadeAccessor.Resolve<IPropertyLoadReloadService>();

        /// <summary>Spawning a property into the game world (P5-9b).</summary>
        public static IPropertySpawnService PropertySpawn => FacadeAccessor.Resolve<IPropertySpawnService>();

        /// <summary>Property clean-up, deletion and starship dock return (P5-9b).</summary>
        public static IPropertyMaintenanceService PropertyMaintenance => FacadeAccessor.Resolve<IPropertyMaintenanceService>();

        /// <summary>Property permission checks and entering a property (P5-9b).</summary>
        public static IPropertyAccessService PropertyAccess => FacadeAccessor.Resolve<IPropertyAccessService>();

        /// <summary>Placing a structure from its item (P5-9b).</summary>
        public static IPropertyStructurePlacementService PropertyStructurePlacement => FacadeAccessor.Resolve<IPropertyStructurePlacementService>();

        /// <summary>The property terminals and the property menu (P5-9b).</summary>
        public static IPropertyTerminalService PropertyTerminal => FacadeAccessor.Resolve<IPropertyTerminalService>();

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
