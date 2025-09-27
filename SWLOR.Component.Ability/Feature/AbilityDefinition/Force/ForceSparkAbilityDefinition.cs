using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceSparkAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private const string Tier1Tag = "ABILITY_FORCE_SPARK_1";
        private const string Tier2Tag = "ABILITY_FORCE_SPARK_2";
        private const string Tier3Tag = "ABILITY_FORCE_SPARK_3";

        public ForceSparkAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IMessagingService MessagingService => _serviceProvider.GetRequiredService<IMessagingService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceSpark1(builder);
            ForceSpark2(builder);
            ForceSpark3(builder);

            return builder.Build();
        }
        private void Impact(uint activator, uint target, int dmg, int evaDecrease, int tier, string effectTag, int dc)
        {
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var statService = StatService;
            var combatService = CombatService;
            var messagingService = MessagingService;
            var enmityService = EnmityService;
            var combatPointService = CombatPointService;

            var attack = statService.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defense = statService.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            dmg += (attackerStat * ((tier - 1) / 2)) + attackerStat;
            var damage = combatService.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);


            if (HasMorePowerfulEffect(target, tier,
                    new(Tier1Tag, 1),
                    new(Tier2Tag, 2),
                    new(Tier3Tag, 3)))
            {
                SendMessageToPC(activator, "Your target is already afflicted by a more powerful effect.");
            }
            else
            {
                RemoveEffectByTag(target, Tier1Tag, Tier2Tag, Tier3Tag);

                dc = combatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc, AbilityType.Willpower);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

                if (checkResult == SavingThrowResultType.Failed)
                {
                    var breach = TagEffect(EffectACDecrease(evaDecrease), effectTag);
                    ApplyEffectToObject(DurationType.Temporary, breach, target, 60f);
                    messagingService.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of evasion down.");
                }
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Starburst_Red), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Beam_Silent_Lightning, false, 2f), target);

            if (statService.GetCurrentFP(activator) < 2 + (tier))
            {
                var darkBargain = 7 * ((2 + tier - statService.GetCurrentFP(activator)));
                statService.ReduceFP(activator, statService.GetCurrentFP(activator));
                ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
            }
            else { statService.ReduceFP(activator, 2 + tier); }

            enmityService.ModifyEnmity(activator, target, 150 + damage);
            combatPointService.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void ForceSpark1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceSpark1, PerkType.ForceSpark)
                .Name("Force Spark I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 0, 2, 1, Tier1Tag, 8);
                });
        }

        private void ForceSpark2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceSpark2, PerkType.ForceSpark)
                .Name("Force Spark II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 15, 4, 2, Tier2Tag, 12);
                });
        }

        private void ForceSpark3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceSpark3, PerkType.ForceSpark)
                .Name("Force Spark III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30, 6, 3, Tier3Tag, 14);
                });
        }
    }
}
