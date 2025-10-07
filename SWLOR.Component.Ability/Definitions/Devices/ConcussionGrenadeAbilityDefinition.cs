using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Devices
{
    public class ConcussionGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public ConcussionGrenadeAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
            : base(serviceProvider, statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ConcussionGrenade1(builder);
            ConcussionGrenade2(builder);
            ConcussionGrenade3(builder);

            return builder.Build();
        }
        
        private void Impact(uint activator, uint target, int dmg, int dc)
        {
            if (GetFactionEqual(activator, target))
                return;

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var defense = _statCalculation.CalculateDefense(target);
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Perception, SkillType.Devices);
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
                    const float Duration = 3f;
                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);

                    AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Knockdown);
                }
            }

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
            });

            CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            EnmityService.ModifyEnmity(activator, target, 180);
        }

        private void ConcussionGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ConcussionGrenade1, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ConcussionGrenade, 24f)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffectType.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        Impact(activator, target, perBonus, -1);
                    });
                });
        }

        private void ConcussionGrenade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ConcussionGrenade2, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ConcussionGrenade, 24f)
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
                    var vfx = EffectVisualEffect(VisualEffectType.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var perDMG = perBonus + 15;
                        Impact(activator, target, perDMG, 8);
                    });
                });
        }

        private void ConcussionGrenade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ConcussionGrenade3, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ConcussionGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffectType.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var perDMG = perBonus + 30;
                        Impact(activator, target, perDMG, 12);
                    });
                });
        }
    }
}
