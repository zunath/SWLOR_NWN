using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class FragGrenadeAbilityDefinition: ExplosiveBaseAbilityDefinition
    {

        public FragGrenadeAbilityDefinition(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            FragGrenade1(builder);
            FragGrenade2(builder);
            FragGrenade3(builder);

            return builder.Build();
        }
        
        private void Impact(uint activator, uint target, int dmg, int dc, float bleedLength)
        {
            if (GetFactionEqual(activator, target))
                return;

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            if (dc > 0)
            {
                dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Reflex, dc);
                var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
                if (checkResult == SavingThrowResultType.Failed)
                {
                    StatusEffectService.Apply(activator, target, StatusEffectType.Bleed, bleedLength);
                }
            }

            DelayCommand(0f, () =>
            {
                AssignCommand(activator, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
                });
            });

            CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            EnmityService.ModifyEnmity(activator, target, 320);
        }

        private void FragGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FragGrenade1, PerkType.FragGrenade)
                .Name("Frag Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.FragGrenade, 12f)
                .HasActivationDelay(2f)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Fnf_Fireball), "explosion2", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        Impact(activator, target, perBonus, -1, 0f);
                    });
                });
        }

        private void FragGrenade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FragGrenade2, PerkType.FragGrenade)
                .Name("Frag Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.FragGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Fnf_Fireball), "explosion2", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var perDMG = 20 + (perBonus * 3 / 2);
                        Impact(activator, target, perDMG, 8, 30f);
                    });
                });
        }

        private void FragGrenade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FragGrenade3, PerkType.FragGrenade)
                .Name("Frag Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.FragGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Fnf_Fireball), "explosion2", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var perDMG = 40 + (perBonus * 2);
                        Impact(activator, target, perDMG, 12, 60f);
                    });
                });
        }
    }
}
