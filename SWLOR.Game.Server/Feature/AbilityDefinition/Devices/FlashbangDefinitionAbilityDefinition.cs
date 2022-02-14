﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class FlashbangDefinitionAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            FlashbangGrenade1();
            FlashbangGrenade2();
            FlashbangGrenade3();

            return _builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!HasExplosives(activator))
            {
                return "You have no explosives.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int abReduce)
        {
            if (GetFactionEqual(activator, target))
                return;

            ApplyEffectToObject(DurationType.Temporary, EffectAttackDecrease(abReduce), target, 20f);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 20);
        }

        private void FlashbangGrenade1()
        {
            _builder.Create(FeatType.FlashbangGrenade1, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade I")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 2);
                    });
                });
        }

        private void FlashbangGrenade2()
        {
            _builder.Create(FeatType.FlashbangGrenade2, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade II")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 4);
                    });
                });
        }

        private void FlashbangGrenade3()
        {
            _builder.Create(FeatType.FlashbangGrenade3, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade III")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 6);
                    });
                });
        }
    }
}
