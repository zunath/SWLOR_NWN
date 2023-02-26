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
    public class PoisonBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            PoisonBreath1();
            PoisonBreath2();
            PoisonBreath3();
            PoisonBreath4();
            PoisonBreath5();

            return _builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int dc)
        {
            var baseDC = dc;
            const float ConeSize = 10f;

            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Perception) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Perception) / 2;
            var totalStat = beastStat + beastmasterStat;

            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Invalid);
            var eVFX = EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S);

            var target = GetFirstObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = Stat.GetDefense(target, CombatDamageType.Poison, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        totalStat,
                        defense,
                        defenderStat,
                        0);

                    var eDMG = EffectDamage(damage, DamageType.Acid);
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
                            StatusEffect.Apply(activator, targetCopy, StatusEffectType.Poison, 30f);
                        }
                    });
                }

                target = GetNextObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void PoisonBreath1()
        {
            _builder.Create(FeatType.PoisonBreath1, PerkType.PoisonBreath)
                .Name("Poison Breath I")
                .Level(1)
                .HasRecastDelay(RecastGroup.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 8, -1);
                });
        }
        private void PoisonBreath2()
        {
            _builder.Create(FeatType.PoisonBreath2, PerkType.PoisonBreath)
                .Name("Poison Breath II")
                .Level(2)
                .HasRecastDelay(RecastGroup.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 12, -1);
                });
        }
        private void PoisonBreath3()
        {
            _builder.Create(FeatType.PoisonBreath3, PerkType.PoisonBreath)
                .Name("Poison Breath III")
                .Level(3)
                .HasRecastDelay(RecastGroup.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, 8);
                });
        }
        private void PoisonBreath4()
        {
            _builder.Create(FeatType.PoisonBreath4, PerkType.PoisonBreath)
                .Name("Poison Breath IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 20, 12);
                });
        }
        private void PoisonBreath5()
        {
            _builder.Create(FeatType.PoisonBreath5, PerkType.PoisonBreath)
                .Name("Poison Breath V")
                .Level(5)
                .HasRecastDelay(RecastGroup.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 24, 14);
                });
        }
    }
}
