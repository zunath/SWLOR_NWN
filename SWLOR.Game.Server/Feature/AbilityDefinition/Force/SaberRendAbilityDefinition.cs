using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class SaberRendAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SaberRend1(builder);
            SaberRend2(builder);

            return builder.Build();
        }

        private static void SaberRend1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SaberRend1, PerkType.SaberRend)
                .Name("Saber Rend I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SaberRend, 18f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasCustomValidation(ValidateMeleeWeapon)
                .HasImpactAction(SaberRend1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3)
                .RequirementStamina(1);
        }

        private static void SaberRend2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SaberRend2, PerkType.SaberRend)
                .Name("Saber Rend II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SaberRend, 18f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasCustomValidation(ValidateMeleeWeapon)
                .HasImpactAction(SaberRend2ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4)
                .RequirementStamina(2);
        }

        private static void SaberRend1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                12,
                0,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static void SaberRend2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                24,
                0,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static string ValidateMeleeWeapon(uint activator, uint target, int effectivePerkLevel, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            if (!GetIsObjectValid(weapon))
                return "A melee weapon is required.";

            var baseItem = GetBaseItemType(weapon);
            return Item.OneHandedMeleeItemTypes.Contains(baseItem) ||
                   Item.TwoHandedMeleeItemTypes.Contains(baseItem)
                ? string.Empty
                : "A melee weapon is required.";
        }
    }
}
