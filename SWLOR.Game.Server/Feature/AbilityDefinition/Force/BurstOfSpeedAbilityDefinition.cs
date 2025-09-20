using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Event;

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
            if (HasMorePowerfulEffect(target, tier,
                    new(Tier1Tag, 1),
                    new(Tier2Tag, 2)))
            {
                return "Your target is already enhanced by a more powerful effect.";
            }

            return string.Empty;
        }

        [ScriptHandler(ScriptName.OnBurstOfSpeedApply)]
        public static void ApplyEffect()
        {
            var activeEffect = GetLastRunScriptEffect();
            var target = OBJECT_SELF;
            var tier = Convert.ToInt32(GetEffectString(activeEffect, 0));
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

            var effect = EffectMovementSpeedIncrease(movementIncrease);
            effect = EffectLinkEffects(EffectACIncrease(acIncrease), effect);
            effect = EffectLinkEffects(effect, EffectIcon(EffectIconType.MovementSpeedIncrease));
            effect = TagEffect(effect, tag);

            ApplyEffectToObject(DurationType.Temporary, effect, target, 600f);

            Stat.ApplyPlayerMovementRate(target);
        }

        [ScriptHandler(ScriptName.OnBurstOfSpeedRemoved)]
        public static void RemoveEffect()
        {
            var target = OBJECT_SELF;
            Stat.ApplyPlayerMovementRate(target);
        }

        private static void Impact(uint activator, uint target, int tier, string effectTag)
        {
            RemoveEffectByTag(target, Tier1Tag, Tier2Tag);

            var effect = EffectRunScript("bspeed_apply", "bspeed_removed", string.Empty, 0f, tier.ToString());
            effect = TagEffect(effect, effectTag);
            ApplyEffectToObject(DurationType.Temporary, effect, target, 600f);

            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
            Enmity.ModifyEnmityOnAll(activator, 250);
        }

        private static void BurstOfSpeed1(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed1, PerkType.BurstOfSpeed)
                .Name("Burst of Speed I")
                .Level(1)
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(2)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 1))
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 1, Tier1Tag);
                });
        }
        private static void BurstOfSpeed2(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed2, PerkType.BurstOfSpeed)
                .Name("Burst of Speed II")
                .Level(2)
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(3)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 2))
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 2, Tier2Tag);
                });
        }
    }
}