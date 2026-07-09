using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WardenRendTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.WardenRendTechnique,
                "Warden Rend Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.1f,
                30f,
                7,
                26,
                10,
                typeof(TerrifiedStatusEffect),
                CombatImpactAreaShape.Sphere,
                5.5f,
                0f,
                CombatDamageType.Force,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Aura_Negative_Energy,
                VisualEffect.Vfx_Fnf_Howl_Mind,
                centerOnActivator: true)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .MimicryTechnique(FeatType.WardenRend, 4, 3)
                .HasTargetingSphere(
                    Spell.WardenRendTechnique,
                    5.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
