using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class IonGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            IonGrenade1();
            IonGrenade2();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int dmg, bool appliesStun)
        {
            if (GetFactionEqual(activator, target))
                return;

            const float Duration = 6f;
            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damageType = CombatDamageType.Electrical;
            var defense = Stat.GetDefense(target, damageType, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                attackerStat,
                defense,
                defenderStat,
                0);
            damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);

            var race = GetRacialType(target);
            if (appliesStun &&
                (race == RacialType.Robot ||
                race == RacialType.Droid ||
                race == RacialType.Cyborg))
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(StunnedStatusEffect), Duration, damageType);
            }

            DelayCommand(0f, () =>
            {
                AssignCommand(activator, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
                });
            });

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 350);
        }

        private void IonGrenade1()
        {
            _builder.Create(FeatType.IonGrenade1, PerkType.IonGrenade)
                .Name("Ion Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
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
                        Impact(activator, target, perBonus, false);
                    });
                });
        }

        private void IonGrenade2()
        {
            _builder.Create(FeatType.IonGrenade2, PerkType.IonGrenade)
                .Name("Ion Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
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
                        Impact(activator, target, perDMG, true);
                    });
                });
        }

    }
}
