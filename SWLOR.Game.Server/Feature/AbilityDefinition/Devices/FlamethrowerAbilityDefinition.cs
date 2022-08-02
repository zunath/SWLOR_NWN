﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class FlamethrowerAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Flamethrower1();
            Flamethrower2();
            Flamethrower3();

            return _builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int burningChance)
        {
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ActionPlayAnimation(Animation.CastOutAnimation, 1f, 2.1f);
                ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Flamethrower), activator, 2f);
            });

            var attackerStat = GetAbilityScore( activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var eVFX = EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S);

            var target = GetFirstObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var eDMG = EffectDamage(damage, DamageType.Fire);
                    Enmity.ModifyEnmity(activator, target, 280);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
                    var dTarget = target; // Without this, ApplyEffect doesn't actually work. Don't ask why.

                    DelayCommand(0.1f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, eDMG, dTarget);
                        ApplyEffectToObject(DurationType.Instant, eVFX, dTarget);

                        if (Random.D100(1) <= burningChance)
                        {
                            StatusEffect.Apply(activator, dTarget, StatusEffectType.Burn, 30f);
                        }
                    });
                }

                target = GetNextObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void Flamethrower1()
        {
            _builder.Create(FeatType.Flamethrower1, PerkType.Flamethrower)
                .Name("Flamethrower I")
                .HasRecastDelay(RecastGroup.Flamethrower, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 6, 0);
                });
        }

        private void Flamethrower2()
        {
            _builder.Create(FeatType.Flamethrower2, PerkType.Flamethrower)
                .Name("Flamethrower II")
                .HasRecastDelay(RecastGroup.Flamethrower, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 10, 30);
                });
        }

        private void Flamethrower3()
        {
            _builder.Create(FeatType.Flamethrower3, PerkType.Flamethrower)
                .Name("Flamethrower III")
                .HasRecastDelay(RecastGroup.Flamethrower, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, 50);
                });
        }
    }
}
