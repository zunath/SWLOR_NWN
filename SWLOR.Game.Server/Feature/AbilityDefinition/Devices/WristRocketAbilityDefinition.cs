using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class WristRocketAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            WristRocket1();
            WristRocket2();
            WristRocket3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int dmg, int dc)
        {
            var targetDistance = GetDistanceBetween(activator, target);
            var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv), target);
            });

            if (dc > 0)
            {
                DelayCommand(delay, () =>
                {
                    const float Duration = 3f;
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), target);

                    dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
                    var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                    if(checkResult == SavingThrowResultType.Failed)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);

                        Ability.ApplyTemporaryImmunity(target, Duration, ImmunityType.Knockdown);
                    }
                });
            }
        }

        private void WristRocket1()
        {
            _builder.Create(FeatType.WristRocket1, PerkType.WristRocket)
                .Name("Wrist Rocket I")
                .Level(1)
                .HasRecastDelay(RecastGroup.WristRocket, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasImpactAction((activator,target, _, targetLocation) =>
                {
                    Impact(activator, target, 8, -1);

                    Enmity.ModifyEnmity(activator, target, 180);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }

        private void WristRocket2()
        {
            _builder.Create(FeatType.WristRocket2, PerkType.WristRocket)
                .Name("Wrist Rocket II")
                .Level(2)
                .HasRecastDelay(RecastGroup.WristRocket, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, target, 12, 8);

                    Enmity.ModifyEnmity(activator, target, 280);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }

        private void WristRocket3()
        {
            _builder.Create(FeatType.WristRocket3, PerkType.WristRocket)
                .Name("Wrist Rocket III")
                .Level(3)
                .HasRecastDelay(RecastGroup.WristRocket, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, target, 20, 12);

                    Enmity.ModifyEnmity(activator, target, 380);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }
    }
}
