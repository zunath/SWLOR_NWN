using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// Every service the container knows, one line each, grouped by domain (the P0-8 domain map). Converting a service
    /// adds its line here and its entry to <c>SWLOR.Architecture.Tests/ConvertedServices.json</c>.
    /// Every game service is a singleton: its state lives for the life of the process.
    /// </summary>
    internal static class ServiceRegistrations
    {
        internal static void Register(IServiceCollection services)
        {
            // Core
            services.AddSingleton<IDBService, StaticDBAdapter>(); // temporary: the static DB class, until DB is converted

            // Administration
            services.AddSingleton<IDMBlueprintCatalogService, DMBlueprintCatalogService>();
            services.AddSingleton<IDMCreatorFavoriteService, DMCreatorFavoriteService>();
            services.AddSingleton<IDMCreatureManagerFavoriteService, DMCreatureManagerFavoriteService>();
            services.AddSingleton<IDMObjectDetailsService, DMObjectDetailsService>();
            services.AddSingleton<IDMObjectSearchService, DMObjectSearchService>();
            services.AddSingleton<IDMPlayerDirectoryService, DMPlayerDirectoryService>();
            services.AddSingleton<IDMPreferenceService, DMPreferenceService>();
            services.AddSingleton<IDMPresenceService, DMPresenceService>();

            // Character
            services.AddSingleton<IAchievementService, AchievementService>();

            // Economy
            services.AddSingleton<IBankService, BankService>();
            services.AddSingleton<ICurrencyService, CurrencyService>();
            services.AddSingleton<IGuildService, GuildService>();
            services.AddSingleton<IPlayerMarketService, PlayerMarketService>();

            // Quests
            services.AddSingleton<IFactionService, FactionService>();

            // Social
            services.AddSingleton<IChatCommandService, ChatCommandService>();
            services.AddSingleton<IChatPortraitTellsService, ChatPortraitTellsService>();
            services.AddSingleton<IPartyService, PartyService>();
            services.AddSingleton<IPlayerListService, PlayerListService>();

            // World
            services.AddSingleton<IAreaService, AreaService>();
            services.AddSingleton<IHoloComService, HoloComService>();
            services.AddSingleton<IMusicService, MusicService>();
            services.AddSingleton<INotesService, NotesService>();
            services.AddSingleton<IPlanetService, PlanetService>();
            services.AddSingleton<ITaxiService, TaxiService>();
            services.AddSingleton<ITrapsService, TrapsService>();
            services.AddSingleton<IWeatherService, WeatherService>();
        }
    }
}
