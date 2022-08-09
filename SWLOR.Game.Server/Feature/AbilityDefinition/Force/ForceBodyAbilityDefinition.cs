using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var willpowerBonus = 0.05f * GetAbilityModifier(AbilityType.Willpower, activator);
            if (willpowerBonus > 0.4f)
                willpowerBonus = 0.4f;

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

            ApplyEffectToObject(DurationType.Instant, EffectDamage((int)(GetCurrentHitPoints(activator) * multiplier)), activator);
            Stat.RestoreFP(activator, (int)(GetCurrentHitPoints(activator) * (multiplier + willpowerBonus)));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Odd), target);

            Enmity.ModifyEnmityOnAll(activator, 2000);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceBody1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBody1, PerkType.ForceBody)
                .Name("Force Body I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceBody, 60f * 5f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceBody2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBody2, PerkType.ForceBody)
                .Name("Force Body II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceBody, 60f * 5f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
    }
}