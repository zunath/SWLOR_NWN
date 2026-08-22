using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class GroupDeflectorAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GroupDeflector1(builder);

            return builder.Build();
        }

        private static void GroupDeflector1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GroupDeflector1, PerkType.GroupDeflector)
                .Name("Group Deflector")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_act_shield")
                .HasRecastDelay(RecastGroup.GroupDeflector, 45f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(GroupDeflector1ImpactAction)
                .HasTargetingSphere(
                    Spell.GroupDeflector1,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void GroupDeflector1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                ApplyShieldTemporaryHP(activator, friendly, 70, 8, 30f);
            }
        }

        private static void ApplyShieldTemporaryHP(
            uint activator,
            uint target,
            int flatAmount,
            int percent,
            float durationSeconds)
        {
            var amount = Math.Max(1, flatAmount + GameMath.PercentOf(GetMaxHitPoints(target), percent));
            amount = Ability.ApplyCombatReadinessMagnitude(activator, amount);
            var duration = durationSeconds;

            TemporaryHitPointEffects.ApplyFlat(target, "GROUP_DEFLECTOR", amount, duration);
            DeviceAbilityEffects.ApplyFieldSupportAllyBuffRiders(activator, target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);
        }
    }
}
