using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class BiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IEnmityService _enmityService;

        public BiteAbilityDefinition(
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
            Bite1(builder);
            Bite2(builder);
            Bite3(builder);
            Bite4(builder);
            Bite5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Might, beastmaster) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Might, activator) / 2;

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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Sonic), target);
            });

            _enmityService.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Bite1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite1, PerkType.Bite)
                .Name("Bite I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }

        private void Bite2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite2, PerkType.Bite)
                .Name("Bite II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }

        private void Bite3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite3, PerkType.Bite)
                .Name("Bite III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20);
                });
        }

        private void Bite4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite4, PerkType.Bite)
                .Name("Bite IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 24);
                });
        }

        private void Bite5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite5, PerkType.Bite)
                .Name("Bite V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 28);
                });
        }


    }
}
