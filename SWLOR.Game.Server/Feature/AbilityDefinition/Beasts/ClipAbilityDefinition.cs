using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class ClipAbilityDefinition: IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IAbilityService _abilityService;
        private readonly IEnmityService _enmityService;

        public ClipAbilityDefinition(
            ICombatService combatService, 
            IStatService statService, 
            IAbilityService abilityService, 
            IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _abilityService = abilityService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Clip1(builder);
            Clip2(builder);
            Clip3(builder);
            Clip4(builder);
            Clip5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg, int dc)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Perception) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Perception) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = _statService.GetAttack(activator, AbilityType.Perception, SkillType.Invalid);
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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Green), target);
            });

            const float Duration = 3f;
            dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                _abilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
            }

            _enmityService.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Clip1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip1, PerkType.Clip)
                .Name("Clip I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 10, 8);
                });
        }
        private void Clip2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip2, PerkType.Clip)
                .Name("Clip II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12, 10);
                });
        }
        private void Clip3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip3, PerkType.Clip)
                .Name("Clip III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14, 12);
                });
        }
        private void Clip4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip4, PerkType.Clip)
                .Name("Clip IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16, 14);
                });
        }
        private void Clip5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip5, PerkType.Clip)
                .Name("Clip V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 18, 16);
                });
        }

    }
}
