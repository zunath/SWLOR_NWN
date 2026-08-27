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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class CoordinatedStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CoordinatedStrike1(builder);
            CoordinatedStrike2(builder);

            return builder.Build();
        }

        private static void CoordinatedStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CoordinatedStrike1, PerkType.CoordinatedStrike)
                .Name("Coordinated Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.CoordinatedStrike, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(CoordinatedStrike1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void CoordinatedStrike2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CoordinatedStrike2, PerkType.CoordinatedStrike)
                .Name("Coordinated Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.CoordinatedStrike, 15f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(CoordinatedStrike2ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void CoordinatedStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyCoordinatedStrike(
                activator,
                target,
                targetLocation,
                18);
        }

        private static void CoordinatedStrike2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyCoordinatedStrike(
                activator,
                target,
                targetLocation,
                30);
        }

        private static void ApplyCoordinatedStrike(uint activator, uint target, Location targetLocation, int baseDamage)
        {
            var master = GetMaster(activator);

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                baseDamage,
                0,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                damagePercentAdjustment: creature =>
                    GetIsObjectValid(master) && Combat.HasRecentDamageTarget(master, creature, 6f) ? 25 : 0);
        }
    }
}
