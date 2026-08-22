using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class StaticBurstTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.StaticBurstTechnique,
                "Static Burst",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.StaticBurst,
                1.4f,
                24f,
                8,
                28,
                30,
                typeof(ShockStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_S,
                VisualEffect.Vfx_Fnf_Storm,
                centerOnActivator: true,
                afterSuccessfulHit: InnateAbility.ChainOnHit(InnateAbilityProfile.Mimicry, 2, 5f, 10, typeof(ShockStatusEffect), 30, CombatDamageType.Electrical))
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .MimicryTechnique(FeatType.StaticBurst, 43, 3)
                .HasTargetingSphere(
                    Spell.StaticBurstTechnique,
                    4.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
