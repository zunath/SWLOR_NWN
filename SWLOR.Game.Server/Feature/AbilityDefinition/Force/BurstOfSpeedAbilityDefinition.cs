using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class BurstOfSpeedAbilityDefinition : IAbilityListDefinition
    {
        private const string Tier1Tag = "EFFECT_BURST_OF_SPEED_1";
        private const string Tier2Tag = "EFFECT_BURST_OF_SPEED_2";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            BurstOfSpeed1(builder);
            BurstOfSpeed2(builder);

            return builder.Build();
        }

        private static string Validation(uint target, int tier)
        {
            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                var tag = GetEffectTag(effect);
                if (tag == Tier2Tag && tier < 2)
                {
                    return "Your target is already enhanced by a more powerful effect.";
                }
            }

            return string.Empty;
        }

        private static void Impact(uint activator, uint target, int tier)
        {
            var movementIncrease = 0;
            var acIncrease = 0;
            var tag = string.Empty;

            switch (tier)
            {
                case 1:
                    movementIncrease = 15;
                    acIncrease = 1;
                    tag = Tier1Tag;
                    break;
                case 2:
                    movementIncrease = 25;
                    acIncrease = 2;
                    tag = Tier2Tag;
                    break;
            }

            RemoveEffectByTag(target, Tier1Tag, Tier2Tag);

            var effect = EffectMovementSpeedIncrease(movementIncrease);
            effect = EffectLinkEffects(EffectACIncrease(acIncrease), effect);
            effect = EffectLinkEffects(effect, EffectIcon(EffectIconType.MovementSpeedIncrease));
            effect = TagEffect(effect, tag);
            ApplyEffectToObject(DurationType.Temporary, effect, target, 600f);
            
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
            Enmity.ModifyEnmityOnAll(activator, 250);
        }

        private static void BurstOfSpeed1(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed1, PerkType.BurstOfSpeed)
                .Name("Burst of Speed I")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(2)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 1))
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 1);
                });
        }
        private static void BurstOfSpeed2(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed2, PerkType.BurstOfSpeed)
                .Name("Burst of Speed II")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(3)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 2))
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 2);
                });
        }
    }
}