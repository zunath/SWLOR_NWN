using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class ExecutePreyAbilityDefinition : IAbilityListDefinition
    {
        private const float LowHPThreshold = 0.35f;
        private const int LowHPDamagePercentBonus = 50;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ExecutePrey1(builder);

            return builder.Build();
        }

        private static void ExecutePrey1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ExecutePrey1, PerkType.ExecutePrey)
                .Name("Execute Prey")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.ExecutePrey, 30f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(ExecutePrey1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ExecutePrey1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                30,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                damagePercentAdjustment: creature => IsLowHP(creature) ? LowHPDamagePercentBonus : 0);
        }

        private static bool IsLowHP(uint target)
        {
            return GetIsObjectValid(target) &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * LowHPThreshold;
        }
    }
}
