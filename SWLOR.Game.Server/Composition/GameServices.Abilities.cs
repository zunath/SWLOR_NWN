using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Impact tracking and the combat impacts content applies (the facade <c>Service.Ability</c> until P2-ZF; its other parts have properties of their own).</summary>
        public static IAbilityService Ability => FacadeAccessor.Resolve<IAbilityService>();

        /// <summary>The activation checks and the last denial, a part of the ability service (P5-3d).</summary>
        public static IAbilityActivationService AbilityActivation => FacadeAccessor.Resolve<IAbilityActivationService>();

        /// <summary>The activation and queued-attack state <c>UsePerkFeat</c> runs on, a part of the ability service (P5-3c).</summary>
        public static IAbilityActivationStateService AbilityActivationState => FacadeAccessor.Resolve<IAbilityActivationStateService>();

        /// <summary>Player auras and the Leadership combat point, a part of the ability service (P5-3c).</summary>
        public static IAbilityAuraService AbilityAura => FacadeAccessor.Resolve<IAbilityAuraService>();

        /// <summary>The catalog of ability definitions, a part of the ability service (P5-3b).</summary>
        public static IAbilityCatalogService AbilityCatalog => FacadeAccessor.Resolve<IAbilityCatalogService>();

        /// <summary>Elapsed recharge on ability icons (the facade <c>Service.AbilityCooldownVisual</c> until P2-ZF).</summary>
        public static IAbilityCooldownVisualService AbilityCooldownVisual => FacadeAccessor.Resolve<IAbilityCooldownVisualService>();

        /// <summary>How a hostile ability's damage reaches its target, applied or queued, a part of the ability service (P5-3e).</summary>
        public static IAbilityDamageDeliveryService AbilityDamageDelivery => FacadeAccessor.Resolve<IAbilityDamageDeliveryService>();

        /// <summary>The damage inputs of an ability hit and the unscaled damage formula, a part of the ability service (P5-3e).</summary>
        public static IAbilityDamageInputService AbilityDamageInput => FacadeAccessor.Resolve<IAbilityDamageInputService>();

        /// <summary>The player and NPC damage formulas of an ability hit, a part of the ability service (P5-3e).</summary>
        public static IAbilityDamageStageService AbilityDamageStage => FacadeAccessor.Resolve<IAbilityDamageStageService>();

        /// <summary>The Dark Force conversion and restoration of an ability impact, a part of the ability service (P5-3e).</summary>
        public static IAbilityDarkForceService AbilityDarkForce => FacadeAccessor.Resolve<IAbilityDarkForceService>();

        /// <summary>Temporary immunities after a control effect, a part of the ability service (P5-3c).</summary>
        public static IAbilityImmunityService AbilityImmunity => FacadeAccessor.Resolve<IAbilityImmunityService>();

        /// <summary>The combat log lines of an ability impact, a part of the ability service (P5-3e).</summary>
        public static IAbilityImpactMessageService AbilityImpactMessage => FacadeAccessor.Resolve<IAbilityImpactMessageService>();

        /// <summary>The status effects of an ability impact, a part of the ability service (P5-3e).</summary>
        public static IAbilityStatusApplicationService AbilityStatusApplication => FacadeAccessor.Resolve<IAbilityStatusApplicationService>();

        /// <summary>The per-target hit roll of a hostile ability, a part of the ability service (P5-3f).</summary>
        public static IAbilityHitRollService AbilityHitRoll => FacadeAccessor.Resolve<IAbilityHitRollService>();

        /// <summary>What a hostile ability's hit does to its target once its damage is known, a part of the ability service (P5-3f).</summary>
        public static IAbilityHostileImpactService AbilityHostileImpact => FacadeAccessor.Resolve<IAbilityHostileImpactService>();

        /// <summary>Begin, recurring, end and abort of each ability impact, a part of the ability service (P5-3f).</summary>
        public static IAbilityImpactLifecycleService AbilityImpactLifecycle => FacadeAccessor.Resolve<IAbilityImpactLifecycleService>();

        /// <summary>How an ability's success visual reaches its recipient, a part of the ability service (P5-3f).</summary>
        public static IAbilityImpactVisualService AbilityImpactVisual => FacadeAccessor.Resolve<IAbilityImpactVisualService>();

        /// <summary>The checks of an activation's target, a part of the ability service (P5-3d).</summary>
        public static IAbilityTargetCheckService AbilityTargetCheck => FacadeAccessor.Resolve<IAbilityTargetCheckService>();

        /// <summary>The targeting overrides that update the client's targeting data (the facade <c>Service.AbilityTargetingCache</c> until P2-ZF).</summary>
        public static IAbilityTargetingCacheService AbilityTargetingCache => FacadeAccessor.Resolve<IAbilityTargetingCacheService>();

        /// <summary>The per-target resolution of a hostile ability impact, a part of the ability service (P5-3f).</summary>
        public static IAbilityTargetImpactService AbilityTargetImpact => FacadeAccessor.Resolve<IAbilityTargetImpactService>();

        /// <summary>The Field Engineer pulse emitters that are active (the facade <c>Service.FieldEngineerPulseEmitters</c> until P2-ZF).</summary>
        public static IFieldEngineerPulseEmittersService FieldEngineerPulseEmitters => FacadeAccessor.Resolve<IFieldEngineerPulseEmittersService>();

        /// <summary>Cooldowns of the Force Control healing riders (the facade <c>Service.ForceControlCooldowns</c> until P2-ZF).</summary>
        public static IForceControlCooldownsService ForceControlCooldowns => FacadeAccessor.Resolve<IForceControlCooldownsService>();

        /// <summary>
        /// Mimicry techniques: witnessing, learning, loadouts and traits (the facade <c>Service.Mimicry</c> until P2-ZF). The
        /// namespace <c>Feature.AbilityDefinition.Mimicry</c> hides an imported property, so callers write <c>GameServices.Mimicry.X</c>.
        /// </summary>
        public static IMimicryService Mimicry => FacadeAccessor.Resolve<IMimicryService>();

        /// <summary>Quick cast targeting preferences on native ability buttons (the facade <c>Service.QuickCast</c> until P2-ZF).</summary>
        public static IQuickCastService QuickCast => FacadeAccessor.Resolve<IQuickCastService>();

        /// <summary>When players and beasts died and when a player was last revived mid-fight (the facade <c>Service.ReviveHistory</c> until P2-ZF).</summary>
        public static IReviveHistoryService ReviveHistory => FacadeAccessor.Resolve<IReviveHistoryService>();

        /// <summary>Twin pistol weapon abilities (the facade <c>Service.TwinPistolAbility</c> until P2-ZH; moved from Combat in P5-3f).</summary>
        public static ITwinPistolAbilityService TwinPistolAbility => FacadeAccessor.Resolve<ITwinPistolAbilityService>();
    }
}
