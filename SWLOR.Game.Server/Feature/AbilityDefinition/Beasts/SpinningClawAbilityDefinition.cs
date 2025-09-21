using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class SpinningClawAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IEnmityService _enmityService;

        public SpinningClawAbilityDefinition(
            ICombatService combatService, 
            IStatService statService, 
            IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SpinningClaw1(builder);
            SpinningClaw2(builder);
            SpinningClaw3(builder);
            SpinningClaw4(builder);
            SpinningClaw5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Agility) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Agility) / 2;

            var totalStat = beastmasterStat + beastStat;
            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.Invalid);


            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            while (GetIsObjectValid(creature) && count < 3)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
                    var attack = _statService.GetAttack(activator, AbilityType.Agility, SkillType.Invalid);
                    var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = _combatService.CalculateDamage(
                        attack,
                        dmg,
                        totalStat,
                        defense,
                        defenderStat,
                        0);

                    var dTarget = creature;

                    DelayCommand(0.1f, () =>
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), dTarget);
                        });
                    });

                    _enmityService.ModifyEnmity(activator, creature, 250 + damage);
                    count++;
                }

                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            }
        }

        private void SpinningClaw1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningClaw1, PerkType.SpinningClaw)
                .Name("Spinning Claw I")
                .Level(1)
                .HasRecastDelay(RecastGroup.SpinningClaw, 2 * 60f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 8);
                });
        }
        private void SpinningClaw2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningClaw2, PerkType.SpinningClaw)
                .Name("Spinning Claw II")
                .Level(2)
                .HasRecastDelay(RecastGroup.SpinningClaw, 2 * 60f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }
        private void SpinningClaw3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningClaw3, PerkType.SpinningClaw)
                .Name("Spinning Claw III")
                .Level(3)
                .HasRecastDelay(RecastGroup.SpinningClaw, 2 * 60f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 15);
                });
        }
        private void SpinningClaw4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningClaw4, PerkType.SpinningClaw)
                .Name("Spinning Claw IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.SpinningClaw, 2 * 60f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 18);
                });
        }
        private void SpinningClaw5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningClaw5, PerkType.SpinningClaw)
                .Name("Spinning Claw V")
                .Level(5)
                .HasRecastDelay(RecastGroup.SpinningClaw, 2 * 60f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 21);
                });
        }

    }
}
