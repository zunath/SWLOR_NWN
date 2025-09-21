
using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class ForceTouchAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IEnmityService _enmityService;

        public ForceTouchAbilityDefinition(
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
            ForceTouch1(builder);
            ForceTouch2(builder);
            ForceTouch3(builder);
            ForceTouch4(builder);
            ForceTouch5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Willpower) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Willpower) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = _statService.GetAttack(activator, AbilityType.Willpower, SkillType.Invalid);
            var defense = _statService.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);

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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Mind), target);
            });

            _enmityService.ModifyEnmity(activator, target, 250 + damage);
        }

        private void ForceTouch1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch1, PerkType.ForceTouch)
                .Name("Force Touch I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }

        private void ForceTouch2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch2, PerkType.ForceTouch)
                .Name("Force Touch II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }

        private void ForceTouch3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch3, PerkType.ForceTouch)
                .Name("Force Touch III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20);
                });
        }

        private void ForceTouch4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch4, PerkType.ForceTouch)
                .Name("Force Touch IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 24);
                });
        }

        private void ForceTouch5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch5, PerkType.ForceTouch)
                .Name("Force Touch V")
                .Level(5)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 28);
                });
        }


    }
}
