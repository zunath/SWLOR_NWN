using System.Collections.Generic;
using SWLOR.Game.Server.Service;


using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class ClawAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IStatusEffectService _statusEffectService;

        public ClawAbilityDefinition(ICombatService combatService, IStatService statService, IStatusEffectService statusEffectService)
        {
            _combatService = combatService;
            _statService = statService;
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Claw1();
            Claw2();
            Claw3();
            Claw4();
            Claw5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg, int dc, int level)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Might) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Might) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

            var damage = _combatService.CalculateDamage(
                attack,
                dmg,
                totalStat,
                defense,
                defenderStat,
                0
            );

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Spark_Small), target);
            });
            
            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                _statusEffectService.Apply(activator, target, StatusEffectType.Bleed, 30f, level);
            }

            Enmity.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Claw1()
        {
            _builder.Create(FeatType.Claw1, PerkType.Claw)
                .Name("Claw I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 8, 8, level);
                });
        }
        private void Claw2()
        {
            _builder.Create(FeatType.Claw2, PerkType.Claw)
                .Name("Claw II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 11, 10, level);
                });
        }
        private void Claw3()
        {
            _builder.Create(FeatType.Claw3, PerkType.Claw)
                .Name("Claw III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14, 12, level);
                });
        }
        private void Claw4()
        {
            _builder.Create(FeatType.Claw4, PerkType.Claw)
                .Name("Claw IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 17, 14, level);
                });
        }
        private void Claw5()
        {
            _builder.Create(FeatType.Claw5, PerkType.Claw)
                .Name("Claw V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20, 16, level);
                });
        }

    }
}
