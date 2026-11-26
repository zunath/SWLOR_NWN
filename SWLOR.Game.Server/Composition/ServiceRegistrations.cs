using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// Every service the container knows, one line each, grouped by domain (the domain map). Converting a service
    /// adds its line here and its entry to <c>SWLOR.Architecture.Tests/ConvertedServices.json</c>.
    /// Every game service is a singleton: its state lives for the life of the process.
    /// </summary>
    internal static class ServiceRegistrations
    {
        internal static void Register(IServiceCollection services)
        {
            RegisterProcessAndCore(services);
            RegisterAbilitiesToCharacter(services);
            RegisterCombat(services);
            RegisterCompanionsToItems(services);
            RegisterQuestsToWorld(services);
        }

        private static void RegisterProcessAndCore(IServiceCollection services)
        {
            // Lazy<IXService> for a constructor parameter that would otherwise make a cycle (LazyDependencyCycleTests).
            services.AddSingleton(typeof(Lazy<>), typeof(ContainerLazy<>));

            // Server process: the process-wide event subscriptions and the teardown that lets go of them
            services.AddSingleton<IProcessEventSource, AppDomainProcessEventSource>();
            services.AddSingleton<IProcessEventSubscriptions, ProcessEventSubscriptions>();
            services.AddSingleton<IServerStaticState, ServerStaticState>();
            services.AddSingleton<IServerTeardown, ServerTeardown>();
            // The native hooks the hook files enable at their boot moments: the process's own on the native entry; the host
            // registers the one it installs the hooks through.
            services.AddSingleton<IHookControl, NativeHookTable>();
            // Content: the factory of made content, and the content singletons (written by tools/Refactor/SWLOR.ContentInjection).
            services.AddSingleton<IContentFactory, ContentFactory>();
            ContentRegistrations.Register(services);
            // The weapon appearance definitions the appearance editor reads, built once with the container.
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.IWeaponAppearanceCatalog, SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponAppearanceCatalog>();

            // Core
            // The Redis connection DBService takes; a host that keeps it across hot reloads registers its own.
            services.AddSingleton<IRedisConnectionSource, RedisConnectionSource>();
            services.AddSingleton<IDBService, DBService>();
            services.AddSingleton<IAnimationPlayerService, AnimationPlayerService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();
            services.AddSingleton<IBackgroundJobService, BackgroundJobService>();
            services.AddSingleton<IItemCacheInfo, ItemCacheInfo>();
            services.AddSingleton<ICacheService, CacheService>();
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
        }

        private static void RegisterAbilitiesToCharacter(IServiceCollection services)
        {
            // Abilities
            services.AddSingleton<IAbilityService, AbilityService>();
            services.AddSingleton<IAbilityActivationService, AbilityActivationService>();
            services.AddSingleton<IAbilityActivationStateService, AbilityActivationStateService>();
            services.AddSingleton<IAbilityAuraService, AbilityAuraService>();
            // The same instance, seen through the interface Combat owns for a creature revived in place.
            services.AddSingleton<ICreatureRevivedListener>(provider => provider.GetRequiredService<IAbilityAuraService>());
            services.AddSingleton<IAbilityCatalogService, AbilityCatalogService>();
            // The same instance, seen through the interface Character owns for the domains below Abilities.
            services.AddSingleton<IAbilityCatalog>(provider => provider.GetRequiredService<IAbilityCatalogService>());
            services.AddSingleton<IAbilityCooldownVisualService, AbilityCooldownVisualService>();
            services.AddSingleton<IAbilityDamageDeliveryService, AbilityDamageDeliveryService>();
            services.AddSingleton<IAbilityDamageInputService, AbilityDamageInputService>();
            services.AddSingleton<IAbilityDamageStageService, AbilityDamageStageService>();
            services.AddSingleton<IAbilityDarkForceService, AbilityDarkForceService>();
            services.AddSingleton<IAbilityHitRollService, AbilityHitRollService>();
            services.AddSingleton<IAbilityHostileImpactService, AbilityHostileImpactService>();
            services.AddSingleton<IAbilityImmunityService, AbilityImmunityService>();
            services.AddSingleton<IAbilityImpactLifecycleService, AbilityImpactLifecycleService>();
            services.AddSingleton<IAbilityImpactMessageService, AbilityImpactMessageService>();
            services.AddSingleton<IAbilityImpactVisualService, AbilityImpactVisualService>();
            services.AddSingleton<IAbilityStatusApplicationService, AbilityStatusApplicationService>();
            services.AddSingleton<IAbilityTargetCheckService, AbilityTargetCheckService>();
            services.AddSingleton<IAbilityTargetImpactService, AbilityTargetImpactService>();
            services.AddSingleton<IAbilityTargetingCacheService, AbilityTargetingCacheService>();
            services.AddSingleton<IFieldEngineerPulseEmittersService, FieldEngineerPulseEmittersService>();
            services.AddSingleton<IForceControlCooldownsService, ForceControlCooldownsService>();
            services.AddSingleton<IMimicryService, MimicryService>();
            services.AddSingleton<IQuickCastService, QuickCastService>();
            services.AddSingleton<IReviveHistoryService, ReviveHistoryService>();
            services.AddSingleton<ITwinPistolAbilityService, TwinPistolAbilityService>();

            // Administration
            services.AddSingleton<IDMBlueprintCatalogService, DMBlueprintCatalogService>();
            services.AddSingleton<IDMCreatorFavoriteService, DMCreatorFavoriteService>();
            services.AddSingleton<IDMCreatureManagerFavoriteService, DMCreatureManagerFavoriteService>();
            services.AddSingleton<IDMObjectDetailsService, DMObjectDetailsService>();
            services.AddSingleton<IDMObjectSearchService, DMObjectSearchService>();
            services.AddSingleton<IDMPlayerDirectoryService, DMPlayerDirectoryService>();
            services.AddSingleton<IHostedEventsService, HostedEventsService>();
            services.AddSingleton<IDMPreferenceService, DMPreferenceService>();
            services.AddSingleton<IDMPresenceService, DMPresenceService>();
            // The same instance, seen through the DM avatar read the disguise and the item trash own.
            services.AddSingleton<IDMActorResolver>(provider => provider.GetRequiredService<IDMPresenceService>());
            services.AddSingleton<IDMWindowPresence>(provider => provider.GetRequiredService<IDMPresenceService>());

            // Character
            services.AddSingleton<IAchievementService, AchievementService>();
            services.AddSingleton<IActivityService, ActivityService>();
            services.AddSingleton<IAppearanceRenderStateService, AppearanceRenderStateService>();
            services.AddSingleton<IDisguiseService, DisguiseService>();
            services.AddSingleton<IEspionageInfiltrationService, EspionageInfiltrationService>();
            services.AddSingleton<ILanguageService, LanguageService>();
            services.AddSingleton<IMimicryTraitStatsService, MimicryTraitStatsService>();
            services.AddSingleton<IPerkService, PerkService>();
            services.AddSingleton<IPerkStatBonusService, PerkStatBonusService>();
            services.AddSingleton<IPerkForceAffinityService, PerkForceAffinityService>();
            services.AddSingleton<IPerkGrantedFeatService, PerkGrantedFeatService>();
            services.AddSingleton<IPerkRefundService, PerkRefundService>();
            services.AddSingleton<IPlayerDescriptorService, PlayerDescriptorService>();
            services.AddSingleton<IPlayerIntroductionService, PlayerIntroductionService>();
            services.AddSingleton<IPlayerNameService, PlayerNameService>();
            services.AddSingleton<IDisplayNameLookup>(provider => provider.GetRequiredService<IPlayerNameService>());
            services.AddSingleton<IPlayerKnownNameService, PlayerKnownNameService>();
            services.AddSingleton<IPlayerNameOverrideService, PlayerNameOverrideService>();
            services.AddSingleton<IRaceService, RaceService>();
            services.AddSingleton<IRoleplayXPDetectionService, RoleplayXPDetectionService>();
            services.AddSingleton<ISkillService, SkillService>();
            services.AddSingleton<ISkillRankService, SkillRankService>();
            services.AddSingleton<ISkillXPService, SkillXPService>();
            services.AddSingleton<ICreatureStatService, CreatureStatService>();
            services.AddSingleton<IStatService, StatService>();
            services.AddSingleton<IStatRecordService, StatRecordService>();
            services.AddSingleton<IStatusEffectCatalogService, StatusEffectCatalogService>();
            services.AddSingleton<IStatusEffectService, StatusEffectService>();
            // Part 7 of the status effect split: login, logout, restart and reload; it registers as the holder of logged-out effects.
            services.AddSingleton<IStatusEffectSessionService, StatusEffectSessionService>();
            // Part 3 of the status effect split: applying a status effect.
            services.AddSingleton<IStatusEffectApplicationService, StatusEffectApplicationService>();
            // Part 4 of the status effect split: the timing, refresh, restart and extension of a status effect.
            services.AddSingleton<IStatusEffectDurationService, StatusEffectDurationService>();
            // Part 6 of the status effect split: the damage and attack notices and the native attack swing.
            services.AddSingleton<IStatusEffectCombatEventService, StatusEffectCombatEventService>();
            // Part 5 of the status effect split: the native effect, the three effect scripts and the ticks.
            services.AddSingleton<IStatusEffectNativeService, StatusEffectNativeService>();
            services.AddSingleton<ITemporaryStatModifierService, TemporaryStatModifierService>();
            services.AddSingleton<ITintMapStateService, TintMapStateService>();
        }

        private static void RegisterCombat(IServiceCollection services)
        {
            // Combat
            // The ability impacts in progress, which the ability lifecycle writes and Combat reads; the store takes no service.
            services.AddSingleton<IAbilityImpactState, AbilityImpactStore>();
            // The same instance, seen through the interface Character owns for the active impact.
            services.AddSingleton<IActiveAbilityImpact>(provider => provider.GetRequiredService<IAbilityImpactState>());
            services.AddSingleton<IBattleGridService, BattleGridService>();
            services.AddSingleton<ICombatAbilityActivationService, CombatAbilityActivationService>();
            services.AddSingleton<ICombatAbilityBonusService, CombatAbilityBonusService>();
            services.AddSingleton<ICombatQueuedAbilityBonusService, CombatQueuedAbilityBonusService>();
            services.AddSingleton<ICombatAbilityCostEffectsService, CombatAbilityCostEffectsService>();
            services.AddSingleton<ICombatAbilityDefenseEffectsService, CombatAbilityDefenseEffectsService>();
            services.AddSingleton<ICombatAbilityHitService, CombatAbilityHitService>();
            services.AddSingleton<ICombatAbilityHitAdjustmentService, CombatAbilityHitAdjustmentService>();
            services.AddSingleton<ICombatAbilityImpactService, CombatAbilityImpactService>();
            services.AddSingleton<ICombatAbilityDamageBonusService, CombatAbilityDamageBonusService>();
            services.AddSingleton<ICombatAbilityHitRidersService, CombatAbilityHitRidersService>();
            services.AddSingleton<ICombatAbilityAreaImpactService, CombatAbilityAreaImpactService>();
            services.AddSingleton<ICombatAbilityWeaponRidersService, CombatAbilityWeaponRidersService>();
            services.AddSingleton<ICombatAbilityStatusAppliedService, CombatAbilityStatusAppliedService>();
            services.AddSingleton<ICombatAbilityMagnitudeService, CombatAbilityMagnitudeService>();
            services.AddSingleton<ICombatAbilityOffenseEffectsService, CombatAbilityOffenseEffectsService>();
            services.AddSingleton<ICombatAbilityWeaponActivationService, CombatAbilityWeaponActivationService>();
            services.AddSingleton<ICombatActivityService, CombatActivityService>();
            services.AddSingleton<ICombatAttackTimingService, CombatAttackTimingService>();
            services.AddSingleton<ICombatDamageCatalogService, CombatDamageCatalogService>();
            services.AddSingleton<ICombatDamageModifiersService, CombatDamageModifiersService>();
            services.AddSingleton<ICombatRateModifiersService, CombatRateModifiersService>();
            services.AddSingleton<ICombatRepeatedTargetService, CombatRepeatedTargetService>();
            services.AddSingleton<ICombatDeflectionReactionService, CombatDeflectionReactionService>();
            services.AddSingleton<ICombatDamageTakenService, CombatDamageTakenService>();
            services.AddSingleton<ICombatDamageTakenEffectsService, CombatDamageTakenEffectsService>();
            services.AddSingleton<ICombatDamageReflectionService, CombatDamageReflectionService>();
            services.AddSingleton<ICombatGuardedHitService, CombatGuardedHitService>();
            services.AddSingleton<ICombatGuardedHitEffectsService, CombatGuardedHitEffectsService>();
            services.AddSingleton<ICombatGuardRetaliationService, CombatGuardRetaliationService>();
            services.AddSingleton<ICombatDamageRollService, CombatDamageRollService>();
            services.AddSingleton<ICombatDefenseMemoryService, CombatDefenseMemoryService>();
            services.AddSingleton<ICombatFeedbackService, CombatFeedbackService>();
            services.AddSingleton<ICombatDealtEffectsService, CombatDealtEffectsService>();
            services.AddSingleton<ICombatTriggeredDamageService, CombatTriggeredDamageService>();
            services.AddSingleton<ICombatAutoAttackEffectsService, CombatAutoAttackEffectsService>();
            services.AddSingleton<ICombatCriticalHitEffectsService, CombatCriticalHitEffectsService>();
            services.AddSingleton<ICombatDefeatedEnemyEffectsService, CombatDefeatedEnemyEffectsService>();
            services.AddSingleton<ICombatHitEffectsService, CombatHitEffectsService>();
            services.AddSingleton<ICombatImpactGeometryService, CombatImpactGeometryService>();
            services.AddSingleton<ICombatPointService, CombatPointService>();
            services.AddSingleton<ICombatQueriesService, CombatQueriesService>();
            services.AddSingleton<ICombatRatingService, CombatRatingService>();
            services.AddSingleton<ICombatRecordAbilityService, CombatRecordAbilityService>();
            services.AddSingleton<ICombatRecordSession, CombatRecordSessionCore>();
            services.AddSingleton<ICombatRecorderService, CombatRecorderService>();
            services.AddSingleton<ICombatRecordDamageService, CombatRecordDamageService>();
            // The same instance, seen through the interfaces the lower domains and the platform own.
            services.AddSingleton<ICombatRecording>(provider => provider.GetRequiredService<ICombatRecorderService>());
            services.AddSingleton<ICharacterRecording>(provider => provider.GetRequiredService<ICombatRecorderService>());
            services.AddSingleton<ICombatRecordingState>(provider => provider.GetRequiredService<ICombatRecordSession>());
            services.AddSingleton<IShutdownStep>(provider => provider.GetRequiredService<ICombatRecorderService>());
            services.AddSingleton<ICombatResourcesService, CombatResourcesService>();
            services.AddSingleton<ICombatService, CombatService>();
            services.AddSingleton<ICombatSuppressionService, CombatSuppressionService>();
            services.AddSingleton<IDeathService, DeathService>();
            services.AddSingleton<IEnmityService, EnmityService>();
            // The same instance, seen through the enmity reads Espionage owns.
            services.AddSingleton<IEspionageEnmityQuery>(provider => provider.GetRequiredService<IEnmityService>());
            services.AddSingleton<IMeleeTechniqueService, MeleeTechniqueService>();
            services.AddSingleton<IOrdinaryAttackTraitsService, OrdinaryAttackTraitsService>();
            services.AddSingleton<IPairedRangedTraitsService, PairedRangedTraitsService>();
            services.AddSingleton<IPoisonsService, PoisonsService>();
            services.AddSingleton<IPowerGloveEquipmentService, PowerGloveEquipmentService>();
            services.AddSingleton<IPvpCombatService, PvpCombatService>();
            services.AddSingleton<IQueuedAttackAnimationService, QueuedAttackAnimationService>();
            services.AddSingleton<IRecastService, RecastService>();
            // The same instance, seen through the recast delays the player introduction owns.
            services.AddSingleton<IRecastDelays>(provider => provider.GetRequiredService<IRecastService>());
            services.AddSingleton<IResistanceService, ResistanceService>();
            // The same instance, seen through the resistance reads the status effects own.
            services.AddSingleton<IStatusResistance>(provider => provider.GetRequiredService<IResistanceService>());
            services.AddSingleton<IResourceRestoredEffectsService, ResourceRestoredEffectsService>();
            // The same instance, seen through the listener the pools in Character own.
            services.AddSingleton<IResourceRestoredListener>(provider => provider.GetRequiredService<IResourceRestoredEffectsService>());
            services.AddSingleton<IStealthService, StealthService>();
            // The same instance, seen through the stealth reads Espionage owns.
            services.AddSingleton<IEspionageStealthQuery>(provider => provider.GetRequiredService<IStealthService>());
            // The same instance, seen through the concealment read the messaging owns.
            services.AddSingleton<IConcealmentQuery>(provider => provider.GetRequiredService<IStealthService>());
            services.AddSingleton<ITargetingService, TargetingService>();
            services.AddSingleton<ITelegraphService, TelegraphService>();
            services.AddSingleton<ITwinPistolEquipmentService, TwinPistolEquipmentService>();
            services.AddSingleton<ITwinPistolPairQuery>(provider => provider.GetRequiredService<ITwinPistolEquipmentService>());
            services.AddSingleton<ITwinPistolMasteryService, TwinPistolMasteryService>();
            services.AddSingleton<IWeaponAttackEligibilityService, WeaponAttackEligibilityService>();
            services.AddSingleton<IWeaponDelayService, WeaponDelayService>();
        }

        private static void RegisterCompanionsToItems(IServiceCollection services)
        {
            // Companions
            // The companion questions Character, Items, Creatures and Combat ask; the lookup takes no service.
            services.AddSingleton<ICompanionLookup, CompanionLookup>();
            services.AddSingleton<IBeastMasteryService, BeastMasteryService>();
            services.AddSingleton<IBeastSpawnService, BeastSpawnService>();
            services.AddSingleton<IBeastEventService, BeastEventService>();
            services.AddSingleton<IBeastIncubationService, BeastIncubationService>();
            // The same instance, seen through the corpse reads the loot owns.
            services.AddSingleton<IBeastCorpseInfo>(provider => provider.GetRequiredService<IBeastMasteryService>());
            services.AddSingleton<IDroidService, DroidService>();
            services.AddSingleton<IDroidEquipmentDiagnosticsService, DroidEquipmentDiagnosticsService>();
            services.AddSingleton<IIncubationFieldNoteService, IncubationFieldNoteService>();
            services.AddSingleton<ITintMapDroidOverrideService, TintMapDroidOverrideService>();
            services.AddSingleton<IProgFxCoatService, ProgFxCoatService>();
            services.AddSingleton<ITintMapApplyService, TintMapApplyService>();
            services.AddSingleton<ITintMapItemColorService, TintMapItemColorService>();
            services.AddSingleton<ITintMapCarryService, TintMapCarryService>();
            services.AddSingleton<ITintMapCatalogService, TintMapCatalogService>();
            services.AddSingleton<ITintMapCreatureColorService, TintMapCreatureColorService>();
            services.AddSingleton<ITintMapService, TintMapService>();

            // Crafting
            services.AddSingleton<ICraftService, CraftService>();
            services.AddSingleton<ICraftEnhancementPropertyService, CraftEnhancementPropertyService>();
            services.AddSingleton<ICraftDeviceService, CraftDeviceService>();
            services.AddSingleton<ICraftMinigameRoutingService, CraftMinigameRoutingService>();
            services.AddSingleton<ICraftQuickCraftService, CraftQuickCraftService>();
            services.AddSingleton<ICraftResearchExperimentService, CraftResearchExperimentService>();
            services.AddSingleton<ICraftSessionsService, CraftSessionsService>();
            services.AddSingleton<ICraftStyleEffectsService, CraftStyleEffectsService>();
            services.AddSingleton<IProspectingService, ProspectingService>();
            services.AddSingleton<IProspectingSessionsService, ProspectingSessionsService>();
            services.AddSingleton<IScavengeService, ScavengeService>();
            services.AddSingleton<IScavengeSessionsService, ScavengeSessionsService>();
            services.AddSingleton<ISlicingService, SlicingService>();
            services.AddSingleton<ISlicingSessionService, SlicingSessionService>();

            // Creatures
            services.AddSingleton<IAIService, AIService>();
            services.AddSingleton<IAICreatureEventService, AICreatureEventService>();
            services.AddSingleton<IAIAggroService, AIAggroService>();
            services.AddSingleton<IAIDecisionService, AIDecisionService>();
            services.AddSingleton<IAIActionService, AIActionService>();
            services.AddSingleton<IAIProfileDescription>(provider => provider.GetRequiredService<IAIService>());
            services.AddSingleton<ICombatLeash>(provider => provider.GetRequiredService<IAIService>());
            services.AddSingleton<ICompanionControlStoreService, CompanionControlStoreService>();
            services.AddSingleton<ICompanionPositioningService, CompanionPositioningService>();
            services.AddSingleton<ICompanionTargetAuthorityService, CompanionTargetAuthorityService>();
            services.AddSingleton<ICompanionTargetQuery>(provider => provider.GetRequiredService<ICompanionTargetAuthorityService>());
            services.AddSingleton<ICompanionControlService, CompanionControlService>();
            services.AddSingleton<INPCGroupService, NPCGroupService>();
            services.AddSingleton<ISpawnRegistryService, SpawnRegistryService>();
            services.AddSingleton<ISpawnObjectService, SpawnObjectService>();
            services.AddSingleton<IDMSpawnPreparation>(provider => provider.GetRequiredService<ISpawnObjectService>());
            services.AddSingleton<ISpawnService, SpawnService>();

            // Economy
            services.AddSingleton<IBankService, BankService>();
            services.AddSingleton<ICasinoSlotService, CasinoSlotService>();
            services.AddSingleton<ICurrencyService, CurrencyService>();
            // The same instance, seen through the balance read the lightsaber workbench owns.
            services.AddSingleton<ICurrencyBalance>(provider => provider.GetRequiredService<ICurrencyService>());
            services.AddSingleton<IGuildService, GuildService>();
            services.AddSingleton<IGuildTaskXpService, GuildTaskXpService>();
            services.AddSingleton<IPlayerMarketService, PlayerMarketService>();

            // Gameplay
            services.AddSingleton<IServerTaskService, ServerTaskService>();

            // Items
            services.AddSingleton<IEquipmentUnequipService, EquipmentUnequipService>();
            services.AddSingleton<IItemService, ItemService>();
            services.AddSingleton<IItemTrashService, ItemTrashService>();
            services.AddSingleton<IKeyItemService, KeyItemService>();
            services.AddSingleton<ILightsaberWorkbenchService, LightsaberWorkbenchService>();
            services.AddSingleton<ILootService, LootService>();
            services.AddSingleton<IOverflowStorageService, OverflowStorageService>();
            services.AddSingleton<IWardrobeService, WardrobeService>();
        }

        private static void RegisterQuestsToWorld(IServiceCollection services)
        {
            // Quests
            services.AddSingleton<ICapstoneProofService, CapstoneProofService>();
            // The same instance, seen through the interface Abilities owns for what a hostile ability does to a target.
            services.AddSingleton<IAbilityImpactObserver>(provider => provider.GetRequiredService<ICapstoneProofService>());
            // The same instance, seen through the interface Combat owns for the record of a beast's hit.
            services.AddSingleton<IBeastDamageRecording>(provider => provider.GetRequiredService<ICapstoneProofService>());
            services.AddSingleton<IFactionService, FactionService>();
            services.AddSingleton<IQuestService, QuestService>();
            // The same instance, seen through the quest advance the twin pistol trials own.
            services.AddSingleton<IQuestAdvancer>(provider => provider.GetRequiredService<IQuestService>());
            services.AddSingleton<IQuestContractBoardService, QuestContractBoardService>();
            services.AddSingleton<IQuestEncounterService, QuestEncounterService>();

            // Space
            services.AddSingleton<ISpaceService, SpaceService>();
            // The space mode read Abilities owns for its activation checks.
            services.AddSingleton<ISpaceModeQuery, SpaceModeQuery>();

            // Social
            services.AddSingleton<IChatCommandService, ChatCommandService>();
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<ICommunicationAudienceService, CommunicationAudienceService>();
            services.AddSingleton<ICommunicationMessageSplitService, CommunicationMessageSplitService>();
            services.AddSingleton<IExtraHotbarsService, ExtraHotbarsService>();
            services.AddSingleton<IExtraHotbarsEditService, ExtraHotbarsEditService>();
            services.AddSingleton<IExtraHotbarsEquipService, ExtraHotbarsEquipService>();
            services.AddSingleton<IExtraHotbarsStatusService, ExtraHotbarsStatusService>();
            services.AddSingleton<IExtraHotbarsWindowService, ExtraHotbarsWindowService>();
            services.AddSingleton<IChatPortraitTellsService, ChatPortraitTellsService>();
            // The same instance, seen through the refusal of a stale portrait tell that Character owns.
            services.AddSingleton<IPortraitTellGuard>(provider => provider.GetRequiredService<IChatPortraitTellsService>());
            services.AddSingleton<IPartyService, PartyService>();
            services.AddSingleton<IPartyRosterService, PartyRosterService>();
            // The same instance, seen through the interface Combat owns for the domains below Social.
            services.AddSingleton<IPartyRoster>(provider => provider.GetRequiredService<IPartyRosterService>());
            services.AddSingleton<IPerformanceService, PerformanceService>();
            services.AddSingleton<IPerformanceAudienceService, PerformanceAudienceService>();
            services.AddSingleton<IPerformanceVenueService, PerformanceVenueService>();
            services.AddSingleton<IPerformanceWindowsService, PerformanceWindowsService>();
            services.AddSingleton<IPerformanceSongService, PerformanceSongService>();
            services.AddSingleton<IPerformanceSnapshotService, PerformanceSnapshotService>();
            services.AddSingleton<IPerformanceInspiredService, PerformanceInspiredService>();
            services.AddSingleton<IPerformanceRiderService, PerformanceRiderService>();
            services.AddSingleton<IPerformanceRegistryService, PerformanceRegistryService>();
            services.AddSingleton<IPerformanceHotbarService, PerformanceHotbarService>();
            services.AddSingleton<IPerformanceEligibilityService, PerformanceEligibilityService>();
            services.AddSingleton<IPerformanceAnimationService, PerformanceAnimationService>();
            services.AddSingleton<IPerformanceWatchingService, PerformanceWatchingService>();
            services.AddSingleton<IPerformanceFlourishService, PerformanceFlourishService>();
            services.AddSingleton<IPerformanceWatchingWindowService, PerformanceWatchingWindowService>();
            services.AddSingleton<IPlayerListService, PlayerListService>();
            // The same instance, seen through the refresh request the holocom owns.
            services.AddSingleton<IPlayerListRefresh>(provider => provider.GetRequiredService<IPlayerListService>());
            services.AddSingleton<IFeatHotBar>(provider => provider.GetRequiredService<IQuickCastService>());
            services.AddSingleton<IStatDrivenTrackerRefresh>(provider => provider.GetRequiredService<ICombatService>());

            // UI
            services.AddSingleton<IGuiService, GuiService>();
            services.AddSingleton<IAchievementNotification, AchievementNotificationWindow>();
            // The same instance, seen through the interface Foundation owns for the domains below the UI.
            services.AddSingleton<IGuiRefreshPublisher>(provider => provider.GetRequiredService<IGuiService>());

            // World
            services.AddSingleton<IAreaService, AreaService>();
            // The same instance, seen through the area reads death and the DM tools own.
            services.AddSingleton<IAreaLookup>(provider => provider.GetRequiredService<IAreaService>());
            services.AddSingleton<ICoxxionBaseDoorsService, CoxxionBaseDoorsService>();
            services.AddSingleton<IFishingService, FishingService>();
            services.AddSingleton<IFishingFightsService, FishingFightsService>();
            services.AddSingleton<IHoloComService, HoloComService>();
            services.AddSingleton<IMusicService, MusicService>();
            // The same instance, seen through the interface Abilities owns for what a hostile ability does to a target.
            services.AddSingleton<IAbilityImpactObserver>(provider => provider.GetRequiredService<IMusicService>());
            services.AddSingleton<INotesService, NotesService>();
            services.AddSingleton<IPlanetService, PlanetService>();
            services.AddSingleton<IPropertyCatalogService, PropertyCatalogService>();
            services.AddSingleton<IPropertyCityGovernanceService, PropertyCityGovernanceService>();
            // The same instance, seen through the city upgrade read the death penalty owns.
            services.AddSingleton<ICityUpgradeLookup>(provider => provider.GetRequiredService<IPropertyCityGovernanceService>());
            services.AddSingleton<IPropertyLoaderService, PropertyLoaderService>();
            services.AddSingleton<IPropertyLoadRegistryService, PropertyLoadRegistryService>();
            services.AddSingleton<IPropertySpawnService, PropertySpawnService>();
            services.AddSingleton<IPropertyLoadReloadService, PropertyLoadReloadService>();
            services.AddSingleton<IPropertyMaintenanceService, PropertyMaintenanceService>();
            services.AddSingleton<IPropertyAccessService, PropertyAccessService>();
            services.AddSingleton<IPropertyStructurePlacementService, PropertyStructurePlacementService>();
            services.AddSingleton<IPropertyTerminalService, PropertyTerminalService>();
            services.AddSingleton<IPropertyService, PropertyService>();
            // The same instance, seen through the property id lookup the craft device terminals own.
            services.AddSingleton<IPropertyIdLookup>(provider => provider.GetRequiredService<IPropertyService>());
            services.AddSingleton<IShuttleArrivalService, ShuttleArrivalService>();
            services.AddSingleton<IShuttleBoardingService, ShuttleBoardingService>();
            services.AddSingleton<IShuttleClockService, ShuttleClockService>();
            services.AddSingleton<IShuttleFlightService, ShuttleFlightService>();
            services.AddSingleton<IShuttlePassengerService, ShuttlePassengerService>();
            services.AddSingleton<IShuttleService, ShuttleService>();
            services.AddSingleton<IShuttleStateService, ShuttleStateService>();
            services.AddSingleton<IShuttleTicketService, ShuttleTicketService>();
            services.AddSingleton<ITaxiService, TaxiService>();
            services.AddSingleton<ITrapsService, TrapsService>();
            services.AddSingleton<IWeatherService, WeatherService>();
        }
    }
}
