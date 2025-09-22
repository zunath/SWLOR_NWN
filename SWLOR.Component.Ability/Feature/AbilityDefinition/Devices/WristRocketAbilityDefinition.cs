using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class WristRocketAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public WristRocketAbilityDefinition(ICombatService combatService, IStatService statService, IAbilityService abilityService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _abilityService = abilityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            WristRocket1(builder);
            WristRocket2(builder);
            WristRocket3(builder);

            return builder.Build();
        }

        private void Impact(uint activator, uint target, int dmg, int dc)
        {
            var targetDistance = GetDistanceBetween(activator, target);
            var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = _statService.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv), target);
            });

            DelayCommand(delay, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), target);

                if (dc > 0)
                {
                    const float Duration = 1f;
                    dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc, AbilityType.Perception);
                    var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                    if (checkResult == SavingThrowResultType.Failed)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);

                        _abilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Knockdown);
                    }
                }
            });
        }

        private void WristRocket1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.WristRocket1, PerkType.WristRocket)
                .Name("Wrist Rocket I")
                .Level(1)
                .HasRecastDelay(RecastGroup.WristRocket, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasImpactAction((activator,target, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    Impact(activator, target, perBonus, -1);

                    _enmityService.ModifyEnmity(activator, target, 180);
                    _combatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }

        private void WristRocket2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.WristRocket2, PerkType.WristRocket)
                .Name("Wrist Rocket II")
                .Level(2)
                .HasRecastDelay(RecastGroup.WristRocket, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    var perDMG = 25 + perBonus;
                    Impact(activator, target, perDMG, 8);

                    _enmityService.ModifyEnmity(activator, target, 280);
                    _combatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }

        private void WristRocket3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.WristRocket3, PerkType.WristRocket)
                .Name("Wrist Rocket III")
                .Level(3)
                .HasRecastDelay(RecastGroup.WristRocket, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    var perDMG = 50 + perBonus * 2;
                    Impact(activator, target, perDMG, 12);

                    _enmityService.ModifyEnmity(activator, target, 380);
                    _combatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                });
        }
    }
}
