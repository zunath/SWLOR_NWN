using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class RestStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly IAbilityService _abilityService;
        private readonly IStatService _statService;
        private readonly IActivityService _activityService;
        private readonly IStatusEffectService _statusEffectService;

        public RestStatusEffectDefinition(IAbilityService abilityService, IStatService statService, IActivityService activityService, IStatusEffectService statusEffectService)
        {
            _abilityService = abilityService;
            _statService = statService;
            _activityService = activityService;
            _statusEffectService = statusEffectService;
        }
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
        public static void RemoveRestOnDamage()
        {
            var player = OBJECT_SELF;
            _statusEffectService.Remove(player, StatusEffectType.Rest);
        }

        /// <summary>
        /// When a player attacks, remove the rest effect
        /// </summary>
        [ScriptHandler<OnInputAttackObjectBefore>]
        public static void RemoveRestOnAttack()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;
            
            _statusEffectService.Remove(player, StatusEffectType.Rest);
        }

        [ScriptHandler<OnModuleEnter>]
        public static void RemoveRestOnLogin()
        {
            var player = GetEnteringObject();
            _statusEffectService.Remove(player, StatusEffectType.Rest);
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
                    _statusEffectService.Remove(target, StatusEffectType.Rest);
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
                        ActionPlayAnimation(Animation.LoopingSitCross, 1f, 9999f);
                    });

                    // Store position the player is at when the rest effect is granted.
                    var position = GetPosition(target);

                    SetLocalFloat(target, "REST_POSITION_X", position.X);
                    SetLocalFloat(target, "REST_POSITION_Y", position.Y);
                    SetLocalFloat(target, "REST_POSITION_Z", position.Z);

                    _activityService.SetBusy(target, ActivityStatusType.Resting);
                    _abilityService.EndConcentrationAbility(target);
                    
                    DelayCommand(0.5f, () => CheckMovement(target));

                    ExecuteScript(ScriptName.OnRestStarted, target);
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

                    var foodEffect = _statusEffectService.GetEffectData<FoodEffectData>(target, StatusEffectType.Food);

                    if (foodEffect != null)
                    {
                        hpAmount += foodEffect.RestRegen * 5;
                        fpAmount += foodEffect.RestRegen * 2;
                        stmAmount += foodEffect.RestRegen * 2;
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(hpAmount), target);
                    _statService.RestoreStamina(target, stmAmount);
                    _statService.RestoreFP(target, fpAmount);
                })
                .RemoveAction((target, effectData) =>
                {
                    // Clean up position information.
                    DeleteLocalFloat(target, "REST_POSITION_X");
                    DeleteLocalFloat(target, "REST_POSITION_Y");
                    DeleteLocalFloat(target, "REST_POSITION_Z");

                    _activityService.ClearBusy(target);
                });
        }
    }
}
