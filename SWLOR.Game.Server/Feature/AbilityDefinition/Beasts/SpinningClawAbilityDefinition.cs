using System.Collections.Generic;
using System.Reflection.Emit;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class SpinningClawAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SpinningClaw1();
            SpinningClaw2();
            SpinningClaw3();
            SpinningClaw4();
            SpinningClaw5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Agility) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Agility) / 2;

            var totalStat = beastmasterStat + beastStat;
            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Invalid);


            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            while (GetIsObjectValid(creature) && count < 3)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
                    var attack = Stat.GetAttack(activator, AbilityType.Agility, SkillType.Invalid);
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        totalStat,
                        defense,
                        defenderStat,
                        0);

                    var dTarget = creature;

                    DelayCommand(0.1f, () =>
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), dTarget));

                    Enmity.ModifyEnmity(activator, creature, 250 + damage);
                    count++;
                }

                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            }
        }

        private void SpinningClaw1()
        {
            _builder.Create(FeatType.SpinningClaw1, PerkType.SpinningClaw)
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
        private void SpinningClaw2()
        {
            _builder.Create(FeatType.SpinningClaw2, PerkType.SpinningClaw)
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
        private void SpinningClaw3()
        {
            _builder.Create(FeatType.SpinningClaw3, PerkType.SpinningClaw)
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
        private void SpinningClaw4()
        {
            _builder.Create(FeatType.SpinningClaw4, PerkType.SpinningClaw)
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
        private void SpinningClaw5()
        {
            _builder.Create(FeatType.SpinningClaw5, PerkType.SpinningClaw)
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
