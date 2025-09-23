using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NWN.Core;
using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Devices;
using SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Force;
using SWLOR.Component.Ability.Feature.AbilityDefinition.General;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership;
using SWLOR.Component.Ability.Feature.AbilityDefinition.MartialArts;
using SWLOR.Component.Ability.Feature.AbilityDefinition.NPC;
using SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Ranged;
using SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded;
using SWLOR.Component.Ability.Infrastructure;
using SWLOR.Component.Ability.Service;
using SWLOR.Component.Admin.Infrastructure;
using SWLOR.Component.Admin.UI.ViewModel;
using SWLOR.Component.AI.Infrastructure;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.Infrastructure;
using SWLOR.Component.Associate.Model;
using SWLOR.Component.Associate.Service;
using SWLOR.Component.Associate.UI.ViewModel;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Feature;
using SWLOR.Component.Combat.Infrastructure;
using SWLOR.Component.Combat.Service;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Dialog;
using SWLOR.Component.Communication.Feature.ChatCommandDefinition;
using SWLOR.Component.Communication.Infrastructure;
using SWLOR.Component.Communication.Service;
using SWLOR.Component.Communication.UI.ViewModel;
using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Feature;
using SWLOR.Component.Crafting.Infrastructure;
using SWLOR.Component.Crafting.Service;
using SWLOR.Component.Crafting.UI.ViewModel;
using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.Dialog;
using SWLOR.Component.Inventory.Feature;
using SWLOR.Component.Inventory.Feature.ItemDefinition;
using SWLOR.Component.Inventory.Feature.SnippetDefinition;
using SWLOR.Component.Inventory.Infrastructure;
using SWLOR.Component.Inventory.Service;
using SWLOR.Component.Inventory.UI.ViewModel;
using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.Dialog;
using SWLOR.Component.Market.Feature;
using SWLOR.Component.Market.Infrastructure;
using SWLOR.Component.Market.Service;
using SWLOR.Component.Market.UI.ViewModel;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Infrastructure;
using SWLOR.Component.Migration.Service;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Feature.PerkDefinition;
using SWLOR.Component.Perk.Infrastructure;
using SWLOR.Component.Perk.Service;
using SWLOR.Component.Perk.UI.ViewModel;
using SWLOR.Component.Properties.Dialog;
using SWLOR.Component.Properties.Infrastructure;
using SWLOR.Component.Properties.UI.ViewModel;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Dialog;
using SWLOR.Component.Quest.Infrastructure;
using SWLOR.Component.Quest.Service;
using SWLOR.Component.Skill.Infrastructure;
using SWLOR.Component.Skill.Service;
using SWLOR.Component.Skill.UI.ViewModel;
using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.Dialog;
using SWLOR.Component.Space.Feature.ShipModuleDefinition;
using SWLOR.Component.Space.Infrastructure;
using SWLOR.Component.Space.Service;
using SWLOR.Component.Space.UI.ViewModel;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition;
using SWLOR.Component.StatusEffect.Infrastructure;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Dialog;
using SWLOR.Component.World.Feature;
using SWLOR.Component.World.Infrastructure;
using SWLOR.Component.World.Service;
using SWLOR.Component.World.UI.ViewModel;
using SWLOR.Game.Server.Server;
using SWLOR.NWN.API;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using SWLOR.Shared.UI.Contracts;
using ScriptExecutionProvider = SWLOR.Game.Server.Server.ScriptExecutionProvider;
using SWLOR.Shared.UI.Infrastructure;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Contracts;

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

            // Dialog Service
            services.AddSingleton<IDialogService, Dialog>();
        }

        private static void AddGameServices(IServiceCollection services)
        {
            // Shared Services
            services.AddCoreServices();
            services.AddEventingServices();
            services.AddCacheServices();
            services.AddUIServices();

            // Component Services
            services.AddAbilityServices();
            services.AddAdminServices();
            services.AddAIServices();
            services.AddAssociateServices();
            services.AddCombatServices();
            services.AddCommunicationServices();
            services.AddCraftingServices();
            services.AddInventoryServices();
            services.AddLanguageServices();
            services.AddMarketServices();
            services.AddMigrationServices();
            services.AddPerkServices();
            services.AddPlayerServices();
            services.AddQuestServices();
            services.AddSkillServices();
            services.AddPropertiesServices();
            services.AddSpaceServices();
            services.AddStatusEffectServices();
            services.AddWorldServices();

            // Game-Specific Services
            AddGameSpecificServices(services);
        }

        private static void AddGameSpecificServices(IServiceCollection services)
        {
            // Dialog Services
            services.AddSingleton<LockedDoorDialog>();
            services.AddSingleton<SliceTerminalDialog>();
            services.AddSingleton<StarportDialog>();
            services.AddSingleton<StarportDockDialog>();
            services.AddSingleton<StarportFlightsDialog>();
            services.AddSingleton<PropertyExitDialog>();
            services.AddSingleton<MedicalRegistrationDialog>();
            services.AddSingleton<DiceDialog>();
            services.AddSingleton<JukeboxDialog>();
            services.AddSingleton<DestroyItemDialog>();
            services.AddSingleton<CoxxionTerminalDialog>();
            services.AddSingleton<MarketDialog>();
            services.AddSingleton<TaxiTerminalDialog>();
            services.AddSingleton<QuestRewardSelectionDialog>();
            services.AddSingleton<XPTomeDialog>();
            services.AddSingleton<HoloComDialog>();


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
            services.AddSingleton<IShipModuleBuilder, ShipModuleBuilder>();

            // Ship Module Definition Services
            services.AddSingleton<IShipModuleListDefinition, AdvancedThrustersModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, WeaponsComputerModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, CapacitorBoosterModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, DamageAmplifierModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, TurboLaserModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, EvasionBoosterModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, HullBoosterModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, ShieldBoosterModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, ReinforcedPlatingModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, ShipArmorModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, TargetingArrayModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, CombatLaserModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, ShipConfigurationModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, TargetingSystemModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, RedundantShieldsModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, AssaultConcussionMissileModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, RepairFieldGeneratorModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, StormCannonModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, ShieldRepairerModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, CapitalPowerDiverterModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, StripMinerModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, CapitalEwarModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, MissileLauncherModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, ProtonBombModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, HypermatterInjectorModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, HullRepairerModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, LaserCannonBatteryModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, QuadLaserCannonModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, MiningLaserModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, BeamCannonModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, IonCannonModuleDefinition>();
            services.AddSingleton<IShipModuleListDefinition, BulwarkShieldGeneratorModuleDefinition>();

            // Player Services
            services.AddSingleton<IPlayerMarketService, PlayerMarket>();
            services.AddSingleton<IBeastMasteryService, BeastMastery>();
            services.AddSingleton<ITargetingService, Targeting>();

            // Inventory Services
            services.AddSingleton<IItemService, Item>();
            services.AddSingleton<ILootService, Loot>();
            services.AddSingleton<IFishingService, Fishing>();




            services.AddSingleton<PlayerInitialization>();
            services.AddSingleton<GuildMasterDialog>();
            services.AddSingleton<PlaceCityHallDialog>();
            services.AddSingleton<CraftViewModel>();
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
            services.AddSingleton<IAbilityListDefinition, SpinningClawAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, WristRocketAbilityDefinition>();
            services.AddSingleton<IAbilityListDefinition, ChargeAbilityDefinition>();

            // Ship Module Definitions
            services.AddSingleton<MiningLaserModuleDefinition>();
            services.AddSingleton<QuadLaserCannonModuleDefinition>();
            services.AddSingleton<MissileLauncherModuleDefinition>();
            services.AddSingleton<ProtonBombModuleDefinition>();
            services.AddSingleton<LaserCannonBatteryModuleDefinition>();
            services.AddSingleton<AssaultConcussionMissileModuleDefinition>();

            // Ability Definitions
            services.AddSingleton<DoubleStrikeAbilityDefinition>();
            services.AddSingleton<CircleSlashAbilityDefinition>();
            services.AddSingleton<SkewerAbilityDefinition>();
            services.AddSingleton<CrossCutAbilityDefinition>();
            services.AddSingleton<QuickDrawAbilityDefinition>();
            services.AddSingleton<SaberStrikeAbilityDefinition>();
            services.AddSingleton<ShieldBashAbilityDefinition>();
            services.AddSingleton<FragGrenadeAbilityDefinition>();
            services.AddSingleton<ClawAbilityDefinition>();
            services.AddSingleton<PoisonStabAbilityDefinition>();
            services.AddSingleton<HackingBladeAbilityDefinition>();
            services.AddSingleton<ConcussionGrenadeAbilityDefinition>();
            services.AddSingleton<ElectricFistAbilityDefinition>();
            services.AddSingleton<FlamethrowerAbilityDefinition>();
            services.AddSingleton<RousingShoutAbilityDefinition>();
            services.AddSingleton<FoodStatusEffectDefinition>();
            services.AddSingleton<ThrowLightsaberAbilityDefinition>();
            services.AddSingleton<IonGrenadeAbilityDefinition>();
            services.AddSingleton<ForceSparkAbilityDefinition>();
            services.AddSingleton<MindTrickAbilityDefinition>();
            services.AddSingleton<ForcePushAbilityDefinition>();
            services.AddSingleton<KoltoRecoveryAbilityDefinition>();
            services.AddSingleton<SpinningClawAbilityDefinition>();
            services.AddSingleton<ShockingSlashAbilityDefinition>();

            // GUI ViewModels
            services.AddTransient<CharacterSheetViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.StablesViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.PlayerStatusViewModel>();
            services.AddTransient<RecipesViewModel>();
            services.AddTransient<ShipManagementViewModel>();
            services.AddTransient<IncubatorViewModel>();
            services.AddTransient<PropertyItemStorageViewModel>();
            services.AddTransient<CraftViewModel>();
            services.AddTransient<IncubatorViewModel>();
            services.AddTransient<Feature.GuiDefinition.ViewModel.StablesViewModel>();
            services.AddTransient<ManageCityViewModel>();
            services.AddTransient<ManageCitizenshipViewModel>();
            services.AddTransient<ManageApartmentViewModel>();
            services.AddTransient<TrainingStoreViewModel>();
            services.AddTransient<RentApartmentViewModel>();
            services.AddTransient<RenameTargetViewModel>();
            services.AddTransient<PropertyPermissionsViewModel>();
            services.AddTransient<ManageStaffViewModel>();
            services.AddTransient<ElectionViewModel>();
            services.AddTransient<CharacterStatRebuildViewModel>();
            services.AddTransient<BugReportViewModel>();
            services.AddTransient<AchievementsViewModel>();
            services.AddTransient<CreatureManagerViewModel>();
            services.AddTransient<NotesViewModel>();
            services.AddTransient<ManageStructuresViewModel>();
            services.AddTransient<CurrenciesViewModel>();
            services.AddTransient<ManageBansViewModel>();
            services.AddTransient<OutfitViewModel>();
            services.AddTransient<MarketListingViewModel>();
            services.AddTransient<MarketBuyViewModel>();
            services.AddTransient<AreaNotesViewModel>();
            services.AddTransient<CustomizeCharacterViewModel>();
            services.AddTransient<CharacterFullRebuildViewModel>();
            services.AddTransient<SkillsViewModel>();
            services.AddTransient<PerksViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Chat Command Services
            services.AddTransient<CharacterChatCommand>();
            services.AddTransient<SystemChatCommand>();
            services.AddTransient<RenameChatCommand>();
            services.AddTransient<DebuggingChatCommand>();
            services.AddTransient<AdminChatCommand>();

            // Core Services
            services.AddSingleton<IObjectVisibilityService, ObjectVisibilityService>();
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
            services.AddSingleton<INPCGroupService, NPCGroup>();

            // Static service conversions
            services.AddSingleton<CombatPoint>();
            services.AddSingleton<BeastMastery>();
            services.AddSingleton<AI>();
            services.AddSingleton<Property>();
            services.AddSingleton<Achievement>();
        }

    }
}
