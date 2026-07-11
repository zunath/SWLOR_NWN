using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WardenWallTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.WardenWallTechnique,
                "Warden Wall",
                Animation.FireForgetTaunt,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.1f,
                30f,
                10,
                28,
                6,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                5.5f,
                0f,
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Magical_Vision,
                VisualEffect.Vfx_Fnf_Howl_War_Cry,
                centerOnActivator: true)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Vitality)
                .MimicryTechnique(FeatType.WardenWall, 4, 3)
                .HasTargetingSphere(
                    Spell.WardenWallTechnique,
                    5.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
