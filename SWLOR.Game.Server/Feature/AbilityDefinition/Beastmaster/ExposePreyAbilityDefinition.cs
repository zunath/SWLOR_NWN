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
    public sealed class ExposePreyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ExposePrey1(builder);

            return builder.Build();
        }

        private static void ExposePrey1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ExposePrey1, PerkType.ExposePrey)
                .Name("Expose Prey")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.PointForward)
                .HasRecastDelay(RecastGroup.ExposePrey, 24f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(ExposePrey1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void ExposePrey1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                20,
                12,
                typeof(ExposedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Imp_Breach);
        }

    }
}
