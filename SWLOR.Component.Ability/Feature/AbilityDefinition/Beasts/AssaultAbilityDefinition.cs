using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Combat.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class AssaultAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly IEnmityService _enmityService;

        public AssaultAbilityDefinition(
            ICombatService combatService, 
            IStatService statService, 
            IStatusEffectService statusEffectService, 
            IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _statusEffectService = statusEffectService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Assault1(builder);
            Assault2(builder);
            Assault3(builder);
            Assault4(builder);
            Assault5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Agility) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Agility) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = _statService.GetAttack(activator, AbilityType.Agility, SkillType.Invalid);
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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Spark_Medium), target);
            });

            _statusEffectService.Apply(activator, activator, StatusEffectType.Assault, 30f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Magblue), activator);

            _enmityService.ModifyEnmity(activator, target, 350 + damage);
        }

        private void Assault1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault1, PerkType.Assault)
                .Name("Assault I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 10);
                });
        }
        private void Assault2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault2, PerkType.Assault)
                .Name("Assault II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14);
                });
        }
        private void Assault3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault3, PerkType.Assault)
                .Name("Assault III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }
        private void Assault4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault4, PerkType.Assault)
                .Name("Assault IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 22);
                });
        }
        private void Assault5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault5, PerkType.Assault)
                .Name("Assault V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 26);
                });
        }

    }
}
