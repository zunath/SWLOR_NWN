using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class CapacitorSurgeTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.CapacitorSurgeTechnique,
                "Capacitor Surge",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.CapacitorSurge,
                1.2f,
                18f,
                5,
                0,
                30,
                typeof(ShockStatusEffect),
                CombatImpactAreaShape.Sphere,
                4f,
                0f,
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_M,
                VisualEffect.Vfx_Fnf_Electric_Explosion,
                centerOnActivator: true)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .MimicryTechnique(FeatType.CapacitorSurge, 2, 2)
                .HasTargetingSphere(
                    Spell.CapacitorSurgeTechnique,
                    4f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
