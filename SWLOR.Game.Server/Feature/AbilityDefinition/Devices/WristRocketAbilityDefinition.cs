﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class WristRocketAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            WristRocket1();
            WristRocket2();
            WristRocket3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, float dmg, int knockdownChance)
        {
            var targetDistance = GetDistanceBetween(activator, target);
            var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));
            var defense = Stat.GetDefense(target, CombatDamageType.Physical) +
                          Stat.GetDefense(target, CombatDamageType.Fire);
            var perception = GetAbilityModifier(AbilityType.Perception, activator);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = Combat.CalculateDamage(dmg, perception, defense, vitality, 0);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv), target);
            });

            DelayCommand(delay, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), target);

                if (Random.D100(1) <= knockdownChance)
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
                }
            });

            Enmity.ModifyEnmity(activator, target, 15);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
        }

        private void WristRocket1()
        {
            _builder.Create(FeatType.WristRocket1, PerkType.WristRocket)
                .Name("Wrist Rocket I")
                .HasRecastDelay(RecastGroup.WristRocket, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasImpactAction((activator,target, _, targetLocation) =>
                {
                    Impact(activator, target, 3.5f, 0);
                });
        }

        private void WristRocket2()
        {
            _builder.Create(FeatType.WristRocket2, PerkType.WristRocket)
                .Name("Wrist Rocket II")
                .HasRecastDelay(RecastGroup.WristRocket, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, target, 6.0f, 30);
                });
        }

        private void WristRocket3()
        {
            _builder.Create(FeatType.WristRocket3, PerkType.WristRocket)
                .Name("Wrist Rocket III")
                .HasRecastDelay(RecastGroup.WristRocket, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, target, 9.5f, 50);
                });
        }
    }
}
