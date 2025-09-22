using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class IonGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IAbilityService _abilityService;

        public IonGrenadeAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService, IAbilityService abilityService) 
            : base(random, itemService, perkService, statService, combatService, combatPointService, enmityService, statusEffectService)
        {
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _abilityService = abilityService;
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            IonGrenade1(builder);
            IonGrenade2(builder);
            IonGrenade3(builder);

            return builder.Build();
        }
        
        private void Impact(uint activator, uint target, int dmg, int dc)
        {
            if (GetFactionEqual(activator, target))
                return;

            const float Duration = 6f;
            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = _statService.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            var race = GetRacialType(target);
            if (dc > 0 &&
                (race == RacialType.Robot ||
                race == RacialType.Droid ||
                race == RacialType.Cyborg))
            {
                dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                if (checkResult == SavingThrowResultType.Failed)
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                    _abilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
                }
            }

            DelayCommand(0f, () =>
            {
                AssignCommand(activator, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
                });
            });

            _combatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            _enmityService.ModifyEnmity(activator, target, 350);
        }

        private void IonGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IonGrenade1, PerkType.IonGrenade)
                .Name("Ion Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var race = GetRacialType(target);
                        if (race == RacialType.Robot || race == RacialType.Droid || race == RacialType.Cyborg)
                        {
                            perBonus *= 3 / 2;
                        }
                        Impact(activator, target, perBonus, -1);
                    });
                });
        }

        private void IonGrenade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IonGrenade2, PerkType.IonGrenade)
                .Name("Ion Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var race = GetRacialType(target);
                        if (race == RacialType.Robot || race == RacialType.Droid || race == RacialType.Cyborg)
                        {
                            perBonus *= 3 / 2;
                        }
                        var perDMG = 15 + (perBonus * 3 / 2);
                        Impact(activator, target, perDMG, 10);
                    });
                });
        }

        private void IonGrenade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IonGrenade3, PerkType.IonGrenade)
                .Name("Ion Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var race = GetRacialType(target);
                        if (race == RacialType.Robot || race == RacialType.Droid || race == RacialType.Cyborg)
                        {
                            perBonus *= 3 / 2;
                        }
                        var perDMG = 30 + (perBonus * 2);
                        Impact(activator, target, perDMG, 14);
                    });
                });
        }
    }
}
