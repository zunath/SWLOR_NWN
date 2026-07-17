using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    public class TacticalEscapeAbilityDefinition : IAbilityListDefinition
    {
        private const string EvasionModifierGroup = "TACTICAL_ESCAPE_EVASION";
        private const float EvasionDurationSeconds = 30f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            TacticalEscape(builder, FeatType.TacticalEscape1, "Tactical Escape I", 1, 8, 35, 8, false);
            TacticalEscape(builder, FeatType.TacticalEscape2, "Tactical Escape II", 2, 12, 60, 12, true);

            return builder.Build();
        }

        private static void TacticalEscape(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina,
            int enmityReductionPercent,
            int evasionPercent,
            bool removesMovementSlows)
        {
            builder
                .Create(feat, PerkType.TacticalEscape)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.TacticalEscape, 45f)
                .SkillType(SkillType.Espionage)
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyTacticalEscape(activator, enmityReductionPercent, evasionPercent, removesMovementSlows))
                .IsCastedAbility()
                .RequirementStamina(stamina);
        }

        private static void ApplyTacticalEscape(
            uint activator,
            int enmityReductionPercent,
            int evasionPercent,
            bool removesMovementSlows)
        {
            Enmity.ReduceEnmityOnAll(activator, enmityReductionPercent);
            TemporaryStatModifier.Replace(
                activator,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                EvasionDurationSeconds,
                EvasionModifierGroup);

            if (removesMovementSlows)
            {
                StatusEffect.RemoveStatusEffectsWithNegativeStat(activator, StatType.MovementSpeedPercentAdjustment);
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Smoke_Puff), activator);
        }
    }
}
