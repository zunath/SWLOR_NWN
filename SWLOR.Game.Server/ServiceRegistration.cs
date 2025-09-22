using Microsoft.Extensions.DependencyInjection;
using NWN.Core;
using SWLOR.Component.Admin.Infrastructure;
using SWLOR.Component.Associate.Infrastructure;
using SWLOR.Component.Combat.Infrastructure;
using SWLOR.Component.Communication.Infrastructure;
using SWLOR.Component.Crafting.Infrastructure;
using SWLOR.Component.Inventory.Infrastructure;
using SWLOR.Component.Language.Infrastructure;
using SWLOR.Component.Market.Infrastructure;
using SWLOR.Component.Player.Infrastructure;
using SWLOR.Component.Properties.Infrastructure;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Dialog;
using SWLOR.Component.Quest.Infrastructure;
using SWLOR.Component.Quest.Service;
using SWLOR.Component.Space.Infrastructure;
using SWLOR.Component.World.Infrastructure;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Feature.AbilityDefinition.General;
using SWLOR.Game.Server.Feature.AbilityDefinition.Leadership;
using SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded;
using SWLOR.Game.Server.Feature.AbilityDefinition.Ranged;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.SnippetDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Server;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.UI.Contracts;
using ScriptExecutionProvider = SWLOR.Game.Server.Server.ScriptExecutionProvider;
using SWLOR.Shared.UI.Infrastructure;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server
{
    /// <summary>
    /// Handles registration of services in the dependency injection container.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all application services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            AddDatabaseServices(services);
            AddInfrastructureServices(services);
            AddServerServices(services);
            AddGameServices(services);

            return services;
        }

        private static void AddDatabaseServices(IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService, DB>();
        }

        private static void AddInfrastructureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, LogService>();
            services.AddSingleton<IAppSettings, AppSettings>();
        }

        private static void AddServerServices(IServiceCollection services)
        {
            // NWN.Core services
            services.AddSingleton<IScriptExecutionProvider, ScriptExecutionProvider>();
            services.AddSingleton<ClosureManager>();
            services.AddSingleton<ICoreFunctionHandler>(provider => provider.GetRequiredService<ClosureManager>());
            services.AddSingleton<IClosureManager>(provider => provider.GetRequiredService<ClosureManager>());

            // SWLOR Services
            services.AddSingleton<IMainLoopProcessor, MainLoopProcessor>();
            services.AddSingleton<INativeInteropManager, NativeInteropManager>();
            services.AddSingleton<IScriptRegistry, ScriptRegistry>();
            services.AddSingleton<IScriptExecutor, ScriptExecutor>();
            services.AddSingleton<IServerBootstrapper, ServerBootstrapper>();
            services.AddSingleton<IServerManager, ServerManager>();
            services.AddSingleton<IEventService, EventService>();
            services.AddSingleton<IEventRegistrationService, EventRegistrationService>();
            services.AddSingleton<IScheduler, Scheduler>();
        }

        private static void AddGameServices(IServiceCollection services)
        {
            // Shared Services
            services.AddCoreServices();
            services.AddEventingServices();
            services.AddCacheServices();
            services.AddUIServices();

            // Component Services
            services.AddAdminServices();
            services.AddAssociateServices();
            services.AddCombatServices();
            services.AddCommunicationServices();
            services.AddCraftingServices();
            services.AddInventoryServices();
            services.AddLanguageServices();
            services.AddMarketServices();
            services.AddPlayerServices();
            services.AddQuestServices();
            services.AddPropertiesServices();
            services.AddSpaceServices();
            services.AddWorldServices();

            // Game-Specific Services
            AddGameSpecificServices(services);
        }

        private static void AddGameSpecificServices(IServiceCollection services)
        {
            // Dialog Services
            services.AddSingleton<Feature.DialogDefinition.LockedDoorDialog>();
            services.AddSingleton<Feature.DialogDefinition.SliceTerminalDialog>();
            services.AddSingleton<Feature.DialogDefinition.StarportDialog>();
            services.AddSingleton<Component.World.Dialog.TaxiTerminalDialog>();
            services.AddSingleton<QuestRewardSelectionDialog>();
            services.AddSingleton<Feature.DialogDefinition.XPTomeDialog>();
            services.AddSingleton<Feature.DialogDefinition.HoloComDialog>();

            // Quest Definition Services
            services.AddSingleton<IQuestListDefinition, DantooineQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, CZ220QuestDefinition>();
            services.AddSingleton<IQuestListDefinition, ViscaraQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, HiddenAccessQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, HutlarQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, SmitheryGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, FabricationGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, EngineeringGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, AgricultureGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, TatooineQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, HuntersGuildQuestDefinition>();
            services.AddSingleton<IQuestListDefinition, KorribanQuestlineDefinition>();
            services.AddSingleton<IQuestListDefinition, MonCalaQuestDefinition>();

        // Feature Services
        services.AddSingleton<MiniMaps>();
        services.AddSingleton<NaturalRegeneration>();
        services.AddSingleton<PlaceableScripts>();
        services.AddSingleton<PersistentMapProgression>();
        services.AddSingleton<UsePerkFeat>();
        services.AddSingleton<EquipmentRestrictions>();
        services.AddSingleton<EquipmentStats>();
        services.AddSingleton<ScavengePoint>();
        services.AddSingleton<StoreManagement>();
        services.AddSingleton<HoloNetTerminal>();
        services.AddSingleton<PlayerStatusWindow>();
        services.AddSingleton<CreatureDeathAnimation>();
        
        
        // Party Service
        services.AddSingleton<IPartyService, PartyService>();
        
        // Combat Services
        services.AddSingleton<ICombatService, Combat>();
        services.AddSingleton<IEnmityService, Enmity>();
        services.AddSingleton<ICombatPointService, CombatPoint>();
        
        // Planet Service
        services.AddSingleton<IPlanetService, PlanetService>();
        
        // ChatCommand Service
        services.AddSingleton<IChatCommandService, ChatCommand>();
        
        // Race Service
        services.AddSingleton<IRace, Race>();
        
        // Droid Personality Services
        services.AddSingleton<DroidGeekyPersonality>();
        services.AddSingleton<DroidPrissyPersonality>();
        services.AddSingleton<DroidSarcasticPersonality>();
        services.AddSingleton<DroidSlangPersonality>();
        services.AddSingleton<DroidBlandPersonality>();
        services.AddSingleton<DroidWorshipfulPersonality>();
        
        // Quest Builder Service
        services.AddTransient<IQuestBuilder, QuestBuilder>();
        services.AddSingleton<IQuestBuilderFactory, QuestBuilderFactory>();
        
        // Quest Component Factories
        services.AddSingleton<IQuestDetailFactory, QuestDetailFactory>();
        services.AddSingleton<IQuestRewardFactory, QuestRewardFactory>();
        services.AddSingleton<IQuestPrerequisiteFactory, QuestPrerequisiteFactory>();
        services.AddSingleton<IQuestObjectiveFactory, QuestObjectiveFactory>();
        
        
        // Fishing Location Builder Service
        services.AddSingleton<IFishingLocationBuilder, FishingLocationBuilder>();
        
        // Communication Services
        services.AddSingleton<ICommunication, Communication>();
        services.AddSingleton<ILanguageService, Language>();
        services.AddSingleton<IMessagingService, Messaging>();
        services.AddSingleton<IHoloComService, HoloCom>();
        
        // World Services
        services.AddSingleton<IPropertyService, Property>();
        services.AddSingleton<IAreaService, Area>();
        services.AddSingleton<IWalkmeshService, Walkmesh>();
        services.AddSingleton<IWeather, Weather>();
        services.AddSingleton<IMigrationService, Migration>();
        
        // Space Services
        services.AddSingleton<ISpaceService, Space>();
        services.AddSingleton<IShipModuleBuilder, Service.SpaceService.ShipModuleBuilder>();
        
        // Ship Module Definition Services
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.AdvancedThrustersModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.WeaponsComputerModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.CapacitorBoosterModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.DamageAmplifierModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.TurboLaserModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.EvasionBoosterModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.HullBoosterModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.ShieldBoosterModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.ReinforcedPlatingModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.ShipArmorModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.TargetingArrayModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.CombatLaserModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.ShipConfigurationModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.TargetingSystemModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.RedundantShieldsModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.AssaultConcussionMissileModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.RepairFieldGeneratorModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.StormCannonModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.ShieldRepairerModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.CapitalPowerDiverterModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.StripMinerModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.CapitalEwarModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.MissileLauncherModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.ProtonBombModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.HypermatterInjectorModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.HullRepairerModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.LaserCannonBatteryModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.QuadLaserCannonModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.MiningLaserModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.BeamCannonModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.IonCannonModuleDefinition>();
        services.AddSingleton<IShipModuleListDefinition, Feature.ShipModuleDefinition.BulwarkShieldGeneratorModuleDefinition>();
        
        // Player Services
        services.AddSingleton<IPlayerMarketService, PlayerMarket>();
        services.AddSingleton<IBeastMasteryService, BeastMastery>();
        services.AddSingleton<ITargetingService, Targeting>();
        
        // Inventory Services
        services.AddSingleton<IItemService, Item>();
        services.AddSingleton<ILootService, Loot>();
        services.AddSingleton<IFishingService, Fishing>();
        
        
        
        
            services.AddSingleton<PlayerInitialization>();
            services.AddSingleton<Feature.DialogDefinition.GuildMasterDialog>();
            services.AddSingleton<Feature.DialogDefinition.PlaceCityHallDialog>();
            services.AddSingleton<Feature.DialogDefinition.StarportDialog>();
        services.AddSingleton<Feature.GuiDefinition.ViewModel.CraftViewModel>();
            services.AddSingleton<IDroid, Droid>();
            services.AddSingleton<Death>();

            // Other Services
            services.AddSingleton<ISnippetListDefinition, KeyItemSnippetDefinition>();
            services.AddSingleton<IItemListDefinition, KeyItemDefinition>();
            services.AddSingleton<AuraStatusEffectDefinition>();
            services.AddSingleton<IItemListDefinition, RecipeItemDefinition>();
            services.AddSingleton<IItemListDefinition, HarvesterItemDefinition>();
            services.AddSingleton<IItemListDefinition, DroidControlItemDefinition>();
            services.AddSingleton<IItemListDefinition, ConsumableItemDefinition>();
            services.AddSingleton<IItemListDefinition, DNAExtractorItemDefinition>();
            services.AddSingleton<IItemListDefinition, BeastEggItemDefinition>();
            services.AddSingleton<IItemListDefinition, FishingRodItemDefinition>();
            services.AddSingleton<IItemListDefinition, KeyItemDefinition>();
            services.AddSingleton<IItemListDefinition, SaberUpgradeItemDefinition>();
            services.AddSingleton<IItemListDefinition, TomeItemDefinition>();
            services.AddSingleton<ISnippetListDefinition, KeyItemSnippetDefinition>();
            services.AddSingleton<IStatusEffectListDefinition, ForceDrainStatusEffectDefinition>();
            services.AddSingleton<IStatusEffectListDefinition, ForceHealStatusEffectDefinition>();
            services.AddSingleton<IStatusEffectListDefinition, DamageStatusEffectDefinition>();
            services.AddSingleton<IPerkListDefinition, TwoHandedPerkDefinition>();
            services.AddSingleton<IPerkListDefinition, RangedPerkDefinition>();
            services.AddSingleton<IPerkListDefinition, MartialArtsPerkDefinition>();
            services.AddSingleton<IAbilityListDefinition, SaberStrikeAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, DashAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, ShieldBashAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, SlamAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, SpikesAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, ConcussionGrenadeAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, SoldiersSpeedAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, SoldiersPrecisionAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, IonGrenadeAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, WristRocketAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, CrescentMoonAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, TranquilizerShotAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, RiotBladeAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, ShieldBashAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, HardSlashAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, DoubleThrustAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, SlamAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, GasBombAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, ConcussionGrenadeAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, AdhesiveGrenadeAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, SoldiersStrikeAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, LegSweepAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, Feature.AbilityDefinition.Beasts.SpinningClawAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, WristRocketAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, ChargeAbilityDefinition>();
            
            // Ship Module Definitions
            services.AddSingleton<Feature.ShipModuleDefinition.MiningLaserModuleDefinition>();
            services.AddSingleton<Feature.ShipModuleDefinition.QuadLaserCannonModuleDefinition>();
            services.AddSingleton<Feature.ShipModuleDefinition.MissileLauncherModuleDefinition>();
            services.AddSingleton<Feature.ShipModuleDefinition.ProtonBombModuleDefinition>();
            services.AddSingleton<Feature.ShipModuleDefinition.LaserCannonBatteryModuleDefinition>();
            services.AddSingleton<Feature.ShipModuleDefinition.AssaultConcussionMissileModuleDefinition>();
            
            // Ability Definitions
            services.AddSingleton<DoubleStrikeAbilityDefinition>();
            services.AddSingleton<CircleSlashAbilityDefinition>();
            services.AddSingleton<SkewerAbilityDefinition>();
            services.AddSingleton<CrossCutAbilityDefinition>();
            services.AddSingleton<QuickDrawAbilityDefinition>();
            services.AddSingleton<SaberStrikeAbilityDefinition>();
            services.AddSingleton<ShieldBashAbilityDefinition>();
            services.AddSingleton<FragGrenadeAbilityDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.Beasts.ClawAbilityDefinition>();
            services.AddSingleton<PoisonStabAbilityDefinition>();
            services.AddSingleton<HackingBladeAbilityDefinition>();
            services.AddSingleton<ConcussionGrenadeAbilityDefinition>();
            services.AddSingleton<ElectricFistAbilityDefinition>();
            services.AddSingleton<FlamethrowerAbilityDefinition>();
            services.AddSingleton<RousingShoutAbilityDefinition>();
            services.AddSingleton<FoodStatusEffectDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.Force.ThrowLightsaberAbilityDefinition>();
            services.AddSingleton<IonGrenadeAbilityDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.Force.ForceSparkAbilityDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.Force.MindTrickAbilityDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.Force.ForcePushAbilityDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.FirstAid.KoltoRecoveryAbilityDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.Beasts.SpinningClawAbilityDefinition>();
            services.AddSingleton<Feature.AbilityDefinition.Beasts.ShockingSlashAbilityDefinition>();
            
            // GUI ViewModels
            services.AddTransient<Feature.GuiDefinition.ViewModel.CharacterSheetViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.StablesViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.PlayerStatusViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.RecipesViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ShipManagementViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.IncubatorViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.PropertyItemStorageViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.CraftViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.IncubatorViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.StablesViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ManageCityViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ManageCitizenshipViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ManageApartmentViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.TrainingStoreViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.RentApartmentViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.RenameTargetViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.PropertyPermissionsViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ManageDMsViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ElectionViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.CharacterStatRebuildViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.BugReportViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.AchievementsViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.CreatureManagerViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.NotesViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ManageStructuresViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.CurrenciesViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.ManageBansViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.OutfitViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.MarketListingViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.MarketBuyViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.AreaNotesViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.CustomizeCharacterViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.CharacterFullRebuildViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.SkillsViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.PerksViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.SettingsViewModel>();
            
            // Chat Command Services
            services.AddTransient<Feature.ChatCommandDefinition.CharacterChatCommand>();
            services.AddTransient<Feature.ChatCommandDefinition.SystemChatCommand>();
            services.AddTransient<Feature.ChatCommandDefinition.RenameChatCommand>();
            services.AddTransient<Feature.ChatCommandDefinition.DebuggingChatCommand>();
            services.AddTransient<Feature.ChatCommandDefinition.AdminChatCommand>();
            
            // Core Services
            services.AddSingleton<IObjectVisibilityService, ObjectVisibilityService>();
            services.AddSingleton<IQuestService, Quest>();
            services.AddSingleton<IAbilityBuilder, AbilityBuilder>();
            services.AddSingleton<IAbilityService, Ability>();
            services.AddSingleton<IPerkService, Perk>();
            services.AddSingleton<ISkillService, SkillService>();
            services.AddSingleton<IStatService, Stat>();
            services.AddSingleton<ICraftService, Craft>();
            services.AddSingleton<IStatusEffectService, StatusEffect>();
            services.AddSingleton<ISpawnService, Spawn>();
            services.AddSingleton<IFactionService, FactionService>();
            services.AddSingleton<IGuildService, GuildService>();
            services.AddSingleton<ICurrencyService, CurrencyService>();
            services.AddSingleton<IActivityService, Activity>();
            services.AddSingleton<IRecastService, Recast>();
            services.AddSingleton<LightsaberAudio>();
            services.AddSingleton<ITimeService, Time>();
            services.AddSingleton<IGenericCacheService, GenericCacheService>();
            services.AddSingleton<IGuiService, GuiService>();
            services.AddSingleton<IAnimationPlayerService, AnimationPlayerService>();
                
            // Static service conversions
            services.AddSingleton<CombatPoint>();
            services.AddSingleton<BeastMastery>();
            services.AddSingleton<AI>();
            services.AddSingleton<Property>();
            services.AddSingleton<Achievement>();
        }
        
    }
}
