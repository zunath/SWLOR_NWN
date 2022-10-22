//using Random = SWLOR.Game.Server.Service.Random;

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
    public class ForceMindAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceMind1(builder);
            ForceMind2(builder);

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
                    multiplier = 0.2f;
                    break;
                case 2:
                    multiplier = 0.4f;
                    break;
                default:
                    break;
            }

             float heal = 0;
            switch (level)
            {
                case 1:
                    heal = 2f;
                    break;
                case 2:
                    heal = 4f;
                    break;
                default:
                    break;
            }

            ApplyEffectToObject(DurationType.Instant, EffectHeal((int)(GetCurrentHitPoints(activator) * heal)), activator);
            Stat.ReduceFP(activator, (int)(GetCurrentHitPoints(activator) * (multiplier + willpowerBonus)));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Odd), target);
            
            Enmity.ModifyEnmityOnAll(activator, 2000);

            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);       
        }

        private static void ForceMind1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceMind1, PerkType.ForceMind)
                .Name("Force Mind I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceMind, 60f * 5f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceMind2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceMind2, PerkType.ForceMind)
                .Name("Force Mind II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceMind, 60f * 5f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
    }
}