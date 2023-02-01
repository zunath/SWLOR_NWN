using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class FlamethrowerAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Flamethrower1();
            Flamethrower2();
            Flamethrower3();

            return _builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int dc)
        {
            var baseDC = dc;
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ActionPlayAnimation(Animation.CastOutAnimation, 1f, 2.1f);
                ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Flamethrower), activator, 2f);
            });

            var attackerStat = GetAbilityScore( activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var eVFX = EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S);

            var target = GetFirstObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var eDMG = EffectDamage(damage, DamageType.Fire);
                    Enmity.ModifyEnmity(activator, target, 280);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
                    
                    // Copying the target is needed because the variable gets adjusted outside the scope of the internal lambda.
                    var targetCopy = target;
                    DelayCommand(0.1f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, eDMG, targetCopy);
                        ApplyEffectToObject(DurationType.Instant, eVFX, targetCopy);

                        dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Reflex, baseDC);
                        var checkResult = ReflexSave(targetCopy, dc, SavingThrowType.None, activator);
                        if (checkResult == SavingThrowResultType.Failed)
                        {
                            StatusEffect.Apply(activator, targetCopy, StatusEffectType.Burn, 30f);
                        }
                    });
                }

                target = GetNextObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void Flamethrower1()
        {
            _builder.Create(FeatType.Flamethrower1, PerkType.Flamethrower)
                .Name("Flamethrower I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Flamethrower, 18f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var perBonus = GetAbilityModifier(AbilityType.Perception, activator);
                    var perDMG = 10 + (perBonus * 2);
                    Impact(activator, targetLocation, perDMG, -1);
                });
        }

        private void Flamethrower2()
        {
            _builder.Create(FeatType.Flamethrower2, PerkType.Flamethrower)
                .Name("Flamethrower II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Flamethrower, 18f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var perBonus = GetAbilityModifier(AbilityType.Perception, activator);
                    var perDMG = 20 + (perBonus * 3);
                    Impact(activator, targetLocation, perDMG, 8);
                });
        }

        private void Flamethrower3()
        {
            _builder.Create(FeatType.Flamethrower3, PerkType.Flamethrower)
                .Name("Flamethrower III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Flamethrower, 18f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var perBonus = GetAbilityModifier(AbilityType.Perception, activator);
                    var perDMG = 30 + (perBonus * 4);
                    Impact(activator, targetLocation, perDMG, 12);
                });
        }
    }
}
