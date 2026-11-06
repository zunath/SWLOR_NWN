using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.UI;
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
            services.AddSingleton<IAnimationPlayerService, AnimationPlayerService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();
            services.AddSingleton<IBackgroundJobService, BackgroundJobService>();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<IConversationMenuService, ConversationMenuService>();
            services.AddSingleton<IConversationService, ConversationService>();
            services.AddSingleton<IMessagingService, MessagingService>();
            services.AddSingleton<IMigrationService, MigrationService>();
            services.AddSingleton<INamedAnimationService, NamedAnimationService>();
            services.AddSingleton<IObjectVisibilityService, ObjectVisibilityService>();
            services.AddSingleton<IPlayerFeedbackService, PlayerFeedbackService>();
            services.AddSingleton<ISnippetService, SnippetService>();
            services.AddSingleton<ITileMagicService, TileMagicService>();
            services.AddSingleton<IWalkmeshService, WalkmeshService>();

            // Abilities
            services.AddSingleton<IAbilityCooldownVisualService, AbilityCooldownVisualService>();
            services.AddSingleton<IMimicryService, MimicryService>();
            services.AddSingleton<IQuickCastService, QuickCastService>();

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
            services.AddSingleton<IActivityService, ActivityService>();
            services.AddSingleton<IPlayerDescriptorService, PlayerDescriptorService>();
            services.AddSingleton<IPlayerIntroductionService, PlayerIntroductionService>();
            services.AddSingleton<IPlayerNameService, PlayerNameService>();
            services.AddSingleton<IRaceService, RaceService>();

            // Combat
            services.AddSingleton<IMeleeTechniqueService, MeleeTechniqueService>();
            services.AddSingleton<IPoisonsService, PoisonsService>();
            services.AddSingleton<IStealthService, StealthService>();

            // Companions
            services.AddSingleton<IBeastMasteryService, BeastMasteryService>();
            services.AddSingleton<IDroidEquipmentDiagnosticsService, DroidEquipmentDiagnosticsService>();
            services.AddSingleton<IIncubationFieldNoteService, IncubationFieldNoteService>();

            // Creatures
            services.AddSingleton<IAIService, AIService>();
            services.AddSingleton<INPCGroupService, NPCGroupService>();
            services.AddSingleton<ISpawnService, SpawnService>();

            // Economy
            services.AddSingleton<IBankService, BankService>();
            services.AddSingleton<ICasinoSlotService, CasinoSlotService>();
            services.AddSingleton<ICurrencyService, CurrencyService>();
            services.AddSingleton<IGuildService, GuildService>();
            services.AddSingleton<IGuildTaskXpService, GuildTaskXpService>();
            services.AddSingleton<IPlayerMarketService, PlayerMarketService>();

            // Items
            services.AddSingleton<IEquipmentUnequipService, EquipmentUnequipService>();
            services.AddSingleton<IItemService, ItemService>();
            services.AddSingleton<IItemTrashService, ItemTrashService>();
            services.AddSingleton<IKeyItemService, KeyItemService>();
            services.AddSingleton<ILightsaberWorkbenchService, LightsaberWorkbenchService>();
            services.AddSingleton<ILootService, LootService>();
            services.AddSingleton<IOverflowStorageService, OverflowStorageService>();
            services.AddSingleton<IWardrobeService, WardrobeService>();

            // Quests
            services.AddSingleton<ICapstoneProofService, CapstoneProofService>();
            services.AddSingleton<IFactionService, FactionService>();
            services.AddSingleton<IQuestService, QuestService>();
            services.AddSingleton<IQuestContractBoardService, QuestContractBoardService>();
            services.AddSingleton<IQuestEncounterService, QuestEncounterService>();

            // Social
            services.AddSingleton<IChatCommandService, ChatCommandService>();
            services.AddSingleton<IChatPortraitTellsService, ChatPortraitTellsService>();
            services.AddSingleton<IPartyService, PartyService>();
            services.AddSingleton<IPerformanceService, PerformanceService>();
            services.AddSingleton<IPerformanceAudienceService, PerformanceAudienceService>();
            services.AddSingleton<IPerformanceVenueService, PerformanceVenueService>();
            services.AddSingleton<IPerformanceWindowsService, PerformanceWindowsService>();
            services.AddSingleton<IPlayerListService, PlayerListService>();

            // UI
            services.AddSingleton<IExtraHotbarsService, ExtraHotbarsService>();
            services.AddSingleton<IGuiService, GuiService>();

            // World
            services.AddSingleton<IAreaService, AreaService>();
            services.AddSingleton<IFishingService, FishingService>();
            services.AddSingleton<IFishingFightsService, FishingFightsService>();
            services.AddSingleton<IHoloComService, HoloComService>();
            services.AddSingleton<IMusicService, MusicService>();
            services.AddSingleton<INotesService, NotesService>();
            services.AddSingleton<IPlanetService, PlanetService>();
            services.AddSingleton<IPropertyCatalogService, PropertyCatalogService>();
            services.AddSingleton<IPropertyCityGovernanceService, PropertyCityGovernanceService>();
            services.AddSingleton<IPropertyLoaderService, PropertyLoaderService>();
            services.AddSingleton<IPropertyService, PropertyService>();
            services.AddSingleton<IShuttleService, ShuttleService>();
            services.AddSingleton<ITaxiService, TaxiService>();
            services.AddSingleton<ITrapsService, TrapsService>();
            services.AddSingleton<IWeatherService, WeatherService>();
        }
    }
}
