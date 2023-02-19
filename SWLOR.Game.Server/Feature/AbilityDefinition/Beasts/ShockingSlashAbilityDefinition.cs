using SWLOR.Game.Server.Service.AbilityService;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class ShockingSlashAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ShockingSlash1();
            ShockingSlash2();
            ShockingSlash3();
            ShockingSlash4();
            ShockingSlash5();

            return _builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int dc, int level)
        {
            var baseDC = dc;
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), targetLocation);
            });

            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Might) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Might) / 2;
            var totalStat = beastStat + beastmasterStat;

            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
            var eVFX = EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Electricity);

            var target = GetFirstObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = Stat.GetDefense(target, CombatDamageType.Electrical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        totalStat,
                        defense,
                        defenderStat,
                        0);

                    var eDMG = EffectDamage(damage, DamageType.Electrical);
                    Enmity.ModifyEnmity(activator, target, 220);

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
                            StatusEffect.Apply(activator, targetCopy, StatusEffectType.Shock, 30f, level);
                        }
                    });
                }

                target = GetNextObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void ShockingSlash1()
        {
            _builder.Create(FeatType.ShockingSlash1, PerkType.ShockingSlash)
                .Name("Shocking Slash I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator,_, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 8, -1, level);
                });
        }
        private void ShockingSlash2()
        {
            _builder.Create(FeatType.ShockingSlash2, PerkType.ShockingSlash)
                .Name("Shocking Slash II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 12, -1, level);
                });
        }
        private void ShockingSlash3()
        {
            _builder.Create(FeatType.ShockingSlash3, PerkType.ShockingSlash)
                .Name("Shocking Slash III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, 8, level);
                });
        }
        private void ShockingSlash4()
        {
            _builder.Create(FeatType.ShockingSlash4, PerkType.ShockingSlash)
                .Name("Shocking Slash IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 20, 12, level);
                });
        }
        private void ShockingSlash5()
        {
            _builder.Create(FeatType.ShockingSlash5, PerkType.ShockingSlash)
                .Name("Shocking Slash V")
                .Level(5)
                .HasRecastDelay(RecastGroup.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 24, 14, level);
                });
        }
    }
}
