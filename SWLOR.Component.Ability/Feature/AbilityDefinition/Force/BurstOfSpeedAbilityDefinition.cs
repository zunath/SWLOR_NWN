using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class BurstOfSpeedAbilityDefinition : IAbilityListDefinition
    {
        private const string Tier1Tag = "EFFECT_BURST_OF_SPEED_1";
        private const string Tier2Tag = "EFFECT_BURST_OF_SPEED_2";
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public BurstOfSpeedAbilityDefinition(IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
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

            var statService = ServiceContainer.GetService<IStatService>();
            statService.ApplyPlayerMovementRate(target);
        }

        [ScriptHandler(ScriptName.OnBurstOfSpeedRemoved)]
        public static void RemoveEffect()
        {
            var target = OBJECT_SELF;
            var statService = ServiceContainer.GetService<IStatService>();
            statService.ApplyPlayerMovementRate(target);
        }

        private void Impact(uint activator, uint target, int tier, string effectTag)
        {
            RemoveEffectByTag(target, Tier1Tag, Tier2Tag);

            var effect = EffectRunScript("bspeed_apply", "bspeed_removed", string.Empty, 0f, tier.ToString());
            effect = TagEffect(effect, effectTag);
            ApplyEffectToObject(DurationType.Temporary, effect, target, 600f);

            _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
            _enmityService.ModifyEnmityOnAll(activator, 250);
        }

        private void BurstOfSpeed1(IAbilityBuilder builder)
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
        private void BurstOfSpeed2(IAbilityBuilder builder)
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
