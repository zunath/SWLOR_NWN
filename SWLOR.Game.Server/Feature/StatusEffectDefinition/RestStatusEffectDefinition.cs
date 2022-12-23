using System;
using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class RestStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Rest(builder);

            return builder.Build();
        }

        /// <summary>
        /// When a player is damaged, remove the rest effect
        /// </summary>
        [NWNEventHandler("pc_damaged")]
        public static void RemoveRestOnDamage()
        {
            var player = OBJECT_SELF;
            StatusEffect.Remove(player, StatusEffectType.Rest);
        }

        [NWNEventHandler("mod_enter")]
        public static void RemoveRestOnLogin()
        {
            var player = GetEnteringObject();
            StatusEffect.Remove(player, StatusEffectType.Rest);
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
                    StatusEffect.Remove(target, StatusEffectType.Rest);
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

                    Activity.SetBusy(target, ActivityStatusType.Resting);
                    Ability.EndConcentrationAbility(target);
                    
                    DelayCommand(0.5f, () => CheckMovement(target));

                    ExecuteScript("rest_started", target);
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

                    var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(target, StatusEffectType.Food);

                    if (foodEffect != null)
                    {
                        hpAmount += foodEffect.RestRegen * 5;
                        fpAmount += foodEffect.RestRegen * 2;
                        stmAmount += foodEffect.RestRegen * 2;
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(hpAmount), target);
                    Stat.RestoreStamina(target, stmAmount);
                    Stat.RestoreFP(target, fpAmount);
                })
                .RemoveAction((target, effectData) =>
                {
                    // Clean up position information.
                    DeleteLocalFloat(target, "REST_POSITION_X");
                    DeleteLocalFloat(target, "REST_POSITION_Y");
                    DeleteLocalFloat(target, "REST_POSITION_Z");

                    Activity.ClearBusy(target);
                });
        }
    }
}
