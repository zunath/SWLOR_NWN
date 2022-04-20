﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core;
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
    public class AdhesiveGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            AdhesiveGrenade1();
            AdhesiveGrenade2();
            AdhesiveGrenade3();

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

        private void Impact(uint activator, uint target, int immobilizeChance, float slowLength)
        {
            if (GetFactionEqual(activator, target))
                return;

            if (Random.D100(1) <= immobilizeChance)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), target, slowLength);
            }
            else
            {
                ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, slowLength);
            }
            
            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 10);
        }

        private void AdhesiveGrenade1()
        {
            _builder.Create(FeatType.AdhesiveGrenade1, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade I")
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
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 0, 12f);
                    });
                });
        }

        private void AdhesiveGrenade2()
        {
            _builder.Create(FeatType.AdhesiveGrenade2, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade II")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 30, 12f);
                    });
                });
        }

        private void AdhesiveGrenade3()
        {
            _builder.Create(FeatType.AdhesiveGrenade3, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade III")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 50, 18);
                    });
                });
        }
    }
}
