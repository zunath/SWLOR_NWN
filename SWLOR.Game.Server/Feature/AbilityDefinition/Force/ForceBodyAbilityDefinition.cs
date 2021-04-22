//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceBodyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceBody1(builder);
            ForceBody2(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            float multiplier = 0;
            switch (level)
            {
                case 1:
                    multiplier = 0.25f;
                    break;
                case 2:
                    multiplier = 0.5f;
                    break;
                default:
                    break;
            }
            // Damage user.
            ApplyEffectToObject(DurationType.Instant, EffectDamage((int)(GetCurrentHitPoints(activator) * multiplier)), activator);

            // Recover FP on target.
            Stat.RestoreFP(activator, (int)(GetCurrentHitPoints(activator) * multiplier));

            // Play VFX
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Odd), target);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceBody1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBody1, PerkType.ForceBody)
                .Name("Force Body I")
                .HasRecastDelay(RecastGroup.ForceBody, 60f * 5f)
                .HasActivationDelay(2.0f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }

        private static void ForceBody2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBody2, PerkType.ForceBody)
                .Name("Force Body II")
                .HasRecastDelay(RecastGroup.ForceBody, 60f * 5f)
                .HasActivationDelay(2.0f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
    }
}