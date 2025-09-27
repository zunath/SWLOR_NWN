using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceLightningAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceLightningAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceLightning1(builder);
            ForceLightning2(builder);
            ForceLightning3(builder);
            ForceLightning4(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var willBonus = GetAbilityScore(activator, AbilityType.Willpower);

            switch (level)
            {
                case 1:
                    dmg = willBonus;
                    break;
                case 2:
                    dmg = 10 + (willBonus * 3 / 2);
                    break;
                case 3:
                    dmg = 20 + (willBonus * 2);
                    break;
                case 4:
                    dmg = 30 + (willBonus * 3);
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Force);
            var count = 0;
            var creature = GetFirstObjectInShape(ShapeType.Sphere, RadiusSize.Huge, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count <= 5)
            {
                if (GetIsReactionTypeHostile(creature, activator) && GetIsDead(creature) == false)
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var defense = StatService.GetDefense(creature, CombatDamageType.Force, AbilityType.Willpower);
                    var attack = StatService.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
                    var defenderStat = GetAbilityScore(creature, AbilityType.Willpower);
                    var damage = CombatService.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var elecBeam = EffectBeam(VisualEffectType.Vfx_Beam_Silent_Lightning, activator, BodyNodeType.Hand, true);
                    var elecBurst = EffectVisualEffect(VisualEffectType.Vfx_Imp_Lightning_S);
                    var dTarget = creature;

                    AssignCommand(activator, () =>
                    {
                        PlaySound("frc_lghtning");
                        ActionPlayAnimation(AnimationType.CastOutAnimation, 1.0f, 3.0f);
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), dTarget);
                        ApplyEffectToObject(DurationType.Temporary, elecBeam, dTarget, 2.5f);
                        ApplyEffectToObject(DurationType.Instant, elecBurst, dTarget);
                    });

                    CombatPointService.AddCombatPoint(activator, creature, SkillType.Force, 3);
                    EnmityService.ModifyEnmity(activator, creature, 100 * level + damage);
                    count++;
                }
                creature = GetNextObjectInShape(ShapeType.Sphere, RadiusSize.Huge, GetLocation(target), true, ObjectType.Creature);
            }
            if (StatService.GetCurrentFP(activator) < 1 + (level * 2))
            {
                var darkBargain = 7 * ((5 + (level * 2) - StatService.GetCurrentFP(activator)));
                StatService.ReduceFP(activator, StatService.GetCurrentFP(activator));
                ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
            }
            else { StatService.ReduceFP(activator, 5 + (level * 2)); }
        }

        private void ForceLightning1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceLightning2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceLightning3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning3, PerkType.ForceLightning)
                .Name("Force Lightning III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceLightning4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning4, PerkType.ForceLightning)
                .Name("Force Lightning IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
    }
}
