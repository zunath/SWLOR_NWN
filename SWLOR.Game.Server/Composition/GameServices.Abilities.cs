using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Ability definitions, activation checks, impact tracking, auras and immunities (the facade <c>Service.Ability</c> until P2-ZF).</summary>
        public static IAbilityService Ability => FacadeAccessor.Resolve<IAbilityService>();

        /// <summary>Elapsed recharge on ability icons (the facade <c>Service.AbilityCooldownVisual</c> until P2-ZF).</summary>
        public static IAbilityCooldownVisualService AbilityCooldownVisual => FacadeAccessor.Resolve<IAbilityCooldownVisualService>();

        /// <summary>The targeting overrides that update the client's targeting data (the facade <c>Service.AbilityTargetingCache</c> until P2-ZF).</summary>
        public static IAbilityTargetingCacheService AbilityTargetingCache => FacadeAccessor.Resolve<IAbilityTargetingCacheService>();

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
    }
}
