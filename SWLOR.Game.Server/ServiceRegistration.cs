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
using SWLOR.Component.Space.Infrastructure;
using SWLOR.Component.World.Infrastructure;
using SWLOR.Game.Server.Server;
using SWLOR.NWN.API;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Core.Async;
using SWLOR.Shared.Core.Configuration;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Infrastructure;
using SWLOR.Shared.Events.Service;
using ScriptExecutionProvider = SWLOR.Game.Server.Server.ScriptExecutionProvider;
using SWLOR.Shared.UI.Infrastructure;

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
            services.AddPropertiesServices();
            services.AddSpaceServices();
            services.AddWorldServices();

            // Game-Specific Services
            AddGameSpecificServices(services);
        }

        private static void AddGameSpecificServices(IServiceCollection services)
        {
            // Dialog Services
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.LockedDoorDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.SliceTerminalDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.StarportDialog>();
            services.AddSingleton<SWLOR.Component.World.Dialog.TaxiTerminalDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.QuestRewardSelectionDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.XPTomeDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.HoloComDialog>();

            // Quest Definition Services
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.DantooineQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.CZ220QuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.ViscaraQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.HiddenAccessQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.HutlarQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.SmitheryGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.FabricationGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.EngineeringGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.AgricultureGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.TatooineQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.HuntersGuildQuestDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.KorribanQuestlineDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.QuestService.IQuestListDefinition, SWLOR.Game.Server.Feature.QuestDefinition.MonCalaQuestDefinition>();

        // Feature Services
        services.AddSingleton<SWLOR.Game.Server.Feature.MiniMaps>();
        services.AddSingleton<SWLOR.Game.Server.Feature.NaturalRegeneration>();
        services.AddSingleton<SWLOR.Game.Server.Feature.PlaceableScripts>();
        services.AddSingleton<SWLOR.Game.Server.Feature.PersistentMapProgression>();
        services.AddSingleton<SWLOR.Game.Server.Feature.UsePerkFeat>();
        services.AddSingleton<SWLOR.Game.Server.Feature.EquipmentRestrictions>();
        services.AddSingleton<SWLOR.Game.Server.Feature.EquipmentStats>();
        services.AddSingleton<SWLOR.Game.Server.Feature.ScavengePoint>();
        services.AddSingleton<SWLOR.Game.Server.Feature.StoreManagement>();
        services.AddSingleton<SWLOR.Game.Server.Feature.HoloNetTerminal>();
        services.AddSingleton<SWLOR.Game.Server.Feature.PlayerStatusWindow>();
        services.AddSingleton<SWLOR.Game.Server.Feature.CreatureDeathAnimation>();
        
        // Party Service
        services.AddSingleton<SWLOR.Shared.Core.Contracts.IPartyService, SWLOR.Game.Server.Service.PartyService>();
        
        // Enmity Service
        services.AddSingleton<SWLOR.Game.Server.Service.IEnmityService, SWLOR.Game.Server.Service.EnmityService>();
        
        // Planet Service
        services.AddSingleton<SWLOR.Game.Server.Service.IPlanetService, SWLOR.Game.Server.Service.PlanetService>();
        
        // ChatCommand Service
        services.AddSingleton<SWLOR.Game.Server.Service.IChatCommandService, SWLOR.Game.Server.Service.ChatCommandService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.PlayerInitialization>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.GuildMasterDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.PlaceCityHallDialog>();
            services.AddSingleton<SWLOR.Game.Server.Feature.DialogDefinition.StarportDialog>();
        services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.StripMinerModuleDefinition>();
        services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.StormCannonModuleDefinition>();
        services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.QuadLaserCannonModuleDefinition>();
        services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CraftViewModel>();
            services.AddSingleton<SWLOR.Game.Server.Service.PlayerMarket>();
            services.AddSingleton<SWLOR.Game.Server.Service.Loot>();
            services.AddSingleton<SWLOR.Game.Server.Service.Fishing>();
            services.AddSingleton<SWLOR.Game.Server.Service.Droid>();
            services.AddSingleton<SWLOR.Game.Server.Service.Death>();

            // Other Services
            services.AddSingleton<SWLOR.Game.Server.Service.SnippetService.ISnippetListDefinition, SWLOR.Game.Server.Feature.SnippetDefinition.KeyItemSnippetDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.KeyItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.StatusEffectDefinition.AuraStatusEffectDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.RecipeItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.HarvesterItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.DroidControlItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.ConsumableItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.DNAExtractorItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.BeastEggItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.FishingRodItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.KeyItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.SaberUpgradeItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.ItemService.IItemListDefinition, SWLOR.Game.Server.Feature.ItemDefinition.TomeItemDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.SnippetService.ISnippetListDefinition, SWLOR.Game.Server.Feature.SnippetDefinition.KeyItemSnippetDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.StatusEffectService.IStatusEffectListDefinition, SWLOR.Game.Server.Feature.StatusEffectDefinition.ForceDrainStatusEffectDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.StatusEffectService.IStatusEffectListDefinition, SWLOR.Game.Server.Feature.StatusEffectDefinition.ForceHealStatusEffectDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.StatusEffectService.IStatusEffectListDefinition, SWLOR.Game.Server.Feature.StatusEffectDefinition.DamageStatusEffectDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.PerkService.IPerkListDefinition, SWLOR.Game.Server.Feature.PerkDefinition.TwoHandedPerkDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.PerkService.IPerkListDefinition, SWLOR.Game.Server.Feature.PerkDefinition.RangedPerkDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.PerkService.IPerkListDefinition, SWLOR.Game.Server.Feature.PerkDefinition.MartialArtsPerkDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.SaberStrikeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.General.DashAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.ShieldBashAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts.SlamAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.NPC.SpikesAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Devices.ConcussionGrenadeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Leadership.SoldiersSpeedAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Leadership.SoldiersPrecisionAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Devices.IonGrenadeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Devices.WristRocketAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded.CrescentMoonAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Ranged.TranquilizerShotAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.RiotBladeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.ShieldBashAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded.HardSlashAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded.DoubleThrustAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts.SlamAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Devices.GasBombAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Devices.ConcussionGrenadeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Devices.AdhesiveGrenadeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Leadership.SoldiersStrikeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts.LegSweepAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Beasts.SpinningClawAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Devices.WristRocketAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Service.AbilityService.IAbilityListDefinition, SWLOR.Game.Server.Feature.AbilityDefinition.Leadership.ChargeAbilityDefinition>();
            
            // Ship Module Definitions
            services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.MiningLaserModuleDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.QuadLaserCannonModuleDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.MissileLauncherModuleDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.ProtonBombModuleDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.LaserCannonBatteryModuleDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.ShipModuleDefinition.AssaultConcussionMissileModuleDefinition>();
            
            // Ability Definitions
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded.DoubleStrikeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded.CircleSlashAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded.SkewerAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded.CrossCutAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Ranged.QuickDrawAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.SaberStrikeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.ShieldBashAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Devices.FragGrenadeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Beasts.ClawAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.PoisonStabAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded.HackingBladeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Devices.ConcussionGrenadeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts.ElectricFistAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Devices.FlamethrowerAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Leadership.RousingShoutAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.StatusEffectDefinition.FoodStatusEffectDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Force.ThrowLightsaberAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Devices.IonGrenadeAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Force.ForceSparkAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Force.MindTrickAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Force.ForcePushAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid.KoltoRecoveryAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Beasts.SpinningClawAbilityDefinition>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.Beasts.ShockingSlashAbilityDefinition>();
            
            // GUI ViewModels
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CharacterSheetViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.StablesViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.PlayerStatusViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.RecipesViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ShipManagementViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.IncubatorViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.PropertyItemStorageViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CraftViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.IncubatorViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.StablesViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ManageCityViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ManageCitizenshipViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ManageApartmentViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.TrainingStoreViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.RentApartmentViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.RenameTargetViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.PropertyPermissionsViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ManageDMsViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ElectionViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CharacterStatRebuildViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.BugReportViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.AchievementsViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CreatureManagerViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.NotesViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ManageStructuresViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CurrenciesViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.ManageBansViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.OutfitViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.MarketListingViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.MarketBuyViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.AreaNotesViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CustomizeCharacterViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.CharacterFullRebuildViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.SkillsViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.PerksViewModel>();
            services.AddTransient<SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.SettingsViewModel>();
            
            // Chat Command Services
            services.AddTransient<SWLOR.Game.Server.Feature.ChatCommandDefinition.CharacterChatCommand>();
            services.AddTransient<SWLOR.Game.Server.Feature.ChatCommandDefinition.SystemChatCommand>();
            services.AddTransient<SWLOR.Game.Server.Feature.ChatCommandDefinition.RenameChatCommand>();
            services.AddTransient<SWLOR.Game.Server.Feature.ChatCommandDefinition.DebuggingChatCommand>();
            services.AddTransient<SWLOR.Game.Server.Feature.ChatCommandDefinition.AdminChatCommand>();
            
            // Core Services
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IObjectVisibilityService, SWLOR.Game.Server.Service.ObjectVisibilityService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IQuestService, SWLOR.Game.Server.Service.Quest>();
            services.AddSingleton<IItemService, SWLOR.Game.Server.Service.Item>();
            services.AddSingleton<SWLOR.Shared.Abstractions.Contracts.ICombatService, SWLOR.Game.Server.Service.Combat>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IAbilityService, SWLOR.Game.Server.Service.Ability>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IPerkService, SWLOR.Game.Server.Service.Perk>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ISkillService, SWLOR.Game.Server.Service.SkillService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IStatService, SWLOR.Game.Server.Service.Stat>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ICraftService, SWLOR.Game.Server.Service.Craft>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IStatusEffectService, SWLOR.Game.Server.Service.StatusEffect>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ISpawnService, SWLOR.Game.Server.Service.SpawnService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IFactionService, SWLOR.Game.Server.Service.FactionService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IGuildService, SWLOR.Game.Server.Service.GuildService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ICurrencyService, SWLOR.Game.Server.Service.CurrencyService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ILanguageService, SWLOR.Game.Server.Service.Language>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IActivityService, SWLOR.Game.Server.Service.Activity>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IMessagingService, SWLOR.Game.Server.Service.Messaging>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IRecastService, SWLOR.Game.Server.Service.Recast>();
            services.AddSingleton<SWLOR.Game.Server.Feature.LightsaberAudio>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ITimeService, SWLOR.Game.Server.Service.Time>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IPropertyService, SWLOR.Game.Server.Service.PropertyService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ISpaceService, SWLOR.Game.Server.Service.Space>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IBeastMasteryService, SWLOR.Game.Server.Service.BeastMasteryService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IPlayerMarketService, SWLOR.Game.Server.Service.PlayerMarketService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ITargetingService, SWLOR.Game.Server.Service.Targeting>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ICombatPointService, SWLOR.Game.Server.Service.CombatPoint>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.ILootService, SWLOR.Game.Server.Service.LootService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IEnmityService, SWLOR.Game.Server.Service.Enmity>();
            services.AddSingleton<SWLOR.Shared.Caching.Contracts.IGenericCacheService, SWLOR.Game.Server.Service.GenericCacheService>();
            services.AddSingleton<SWLOR.Shared.UI.Contracts.IGuiService, SWLOR.Game.Server.Service.GuiService>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IHoloComService, SWLOR.Game.Server.Service.HoloCom>();
            services.AddSingleton<SWLOR.Shared.Core.Contracts.IAnimationPlayerService, SWLOR.Game.Server.Service.AnimationPlayerService>();
            
        // Static service conversions
        services.AddSingleton<SWLOR.Game.Server.Service.CombatPoint>();
        services.AddSingleton<SWLOR.Game.Server.Service.BeastMastery>();
        services.AddSingleton<SWLOR.Game.Server.Service.AI>();
        services.AddSingleton<SWLOR.Game.Server.Service.Property>();
        services.AddSingleton<SWLOR.Game.Server.Service.Achievement>();
        }
        
    }
}
