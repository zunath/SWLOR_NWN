using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class RestStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;

        public RestStatusEffectDefinition(IServiceProvider serviceProvider, IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            _eventAggregator = eventAggregator;
            
            // Initialize lazy services
            _abilityService = new Lazy<IAbilityService>(() => _serviceProvider.GetRequiredService<IAbilityService>());
            _statService = new Lazy<IStatService>(() => _serviceProvider.GetRequiredService<IStatService>());
            _activityService = new Lazy<IActivityService>(() => _serviceProvider.GetRequiredService<IActivityService>());
            _statusEffectService = new Lazy<IStatusEffectService>(() => _serviceProvider.GetRequiredService<IStatusEffectService>());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IAbilityService> _abilityService;
        private readonly Lazy<IStatService> _statService;
        private readonly Lazy<IActivityService> _activityService;
        private readonly Lazy<IStatusEffectService> _statusEffectService;
        
        private IAbilityService AbilityService => _abilityService.Value;
        private IStatService StatService => _statService.Value;
        private IActivityService ActivityService => _activityService.Value;
        private IStatusEffectService StatusEffectService => _statusEffectService.Value;
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Rest(builder);

            return builder.Build();
        }

        /// <summary>
        /// When a player is damaged, remove the rest effect
        /// </summary>
        [ScriptHandler<OnPlayerDamaged>]
        public void RemoveRestOnDamage()
        {
            var player = OBJECT_SELF;
            StatusEffectService.Remove(player, StatusEffectType.Rest);
        }

        /// <summary>
        /// When a player attacks, remove the rest effect
        /// </summary>
        [ScriptHandler<OnInputAttackObjectBefore>]
        public void RemoveRestOnAttack()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;
            
            StatusEffectService.Remove(player, StatusEffectType.Rest);
        }

        [ScriptHandler<OnModuleEnter>]
        public void RemoveRestOnLogin()
        {
            var player = GetEnteringObject();
            StatusEffectService.Remove(player, StatusEffectType.Rest);
        }

        private void Rest(StatusEffectBuilder builder)
        {
            void CheckMovement(uint target)
            {
                if (!GetIsObjectValid(target) || GetIsDead(target))
                    return;

                var position = GetPosition(target);

                var originalPosition = Vector3(
                    GetLocalFloat(target, "REST_POSITION_X"),
                    GetLocalFloat(target, "REST_POSITION_Y"),
                    GetLocalFloat(target, "REST_POSITION_Z"));

                // Player has moved since the effect started. Remove it.
                if (Math.Abs(position.X - originalPosition.X) > 0.1f ||
                    Math.Abs(position.Y - originalPosition.Y) > 0.1f ||
                    Math.Abs(position.Z - originalPosition.Z) > 0.1f)
                {
                    StatusEffectService.Remove(target, StatusEffectType.Rest);
                }

                DelayCommand(0.5f, () => CheckMovement(target));
            }

            builder.Create(StatusEffectType.Rest)
                .Name("Rest")
                .EffectIcon(EffectIconType.Fatigue)
                .GrantAction((source, target, length, effectData) =>
                {
                    AssignCommand(target, () =>
                    {
                        ActionPlayAnimation(AnimationType.LoopingSitCross, 1f, 9999f);
                    });

                    // Store position the player is at when the rest effect is granted.
                    var position = GetPosition(target);

                    SetLocalFloat(target, "REST_POSITION_X", position.X);
                    SetLocalFloat(target, "REST_POSITION_Y", position.Y);
                    SetLocalFloat(target, "REST_POSITION_Z", position.Z);

                    ActivityService.SetBusy(target, ActivityStatusType.Resting);
                    AbilityService.EndConcentrationAbility(target);
                    
                    DelayCommand(0.5f, () => CheckMovement(target));

                    _eventAggregator.Publish(new OnPlayerRestStarted(), target);
                })
                .TickAction((source, target, effectData) =>
                {
                    var vitalityBonus = GetAbilityModifier(AbilityType.Vitality, target);
                    if (vitalityBonus < 0)
                        vitalityBonus = 0;

                    var hpAmount = 1 + vitalityBonus * 7;
                    var stmAmount = 1 + vitalityBonus * 3;
                    var fpAmount = 1 + vitalityBonus * 3;

                    // Guard against negative ability modifiers - always give at least 1 HP/FP/STM recovery per tick.
                    if (hpAmount < 1)
                        hpAmount = 1;
                    if (stmAmount < 1)
                        stmAmount = 1;
                    if (fpAmount < 1)
                        fpAmount = 1;

                    var foodEffect = StatusEffectService.GetEffectData<FoodEffectData>(target, StatusEffectType.Food);

                    if (foodEffect != null)
                    {
                        hpAmount += foodEffect.RestRegen * 5;
                        fpAmount += foodEffect.RestRegen * 2;
                        stmAmount += foodEffect.RestRegen * 2;
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(hpAmount), target);
                    StatService.RestoreStamina(target, stmAmount);
                    StatService.RestoreFP(target, fpAmount);
                })
                .RemoveAction((target, effectData) =>
                {
                    // Clean up position information.
                    DeleteLocalFloat(target, "REST_POSITION_X");
                    DeleteLocalFloat(target, "REST_POSITION_Y");
                    DeleteLocalFloat(target, "REST_POSITION_Z");

                    ActivityService.ClearBusy(target);
                });
        }
    }
}
