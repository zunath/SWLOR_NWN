using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class SuppressingShotTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.SuppressingShotTechnique,
                "Suppressing Shot",
                Animation.PointPistol,
                InnateAbilityProfile.Mimicry,
                RecastGroup.SuppressingShot,
                1.2f,
                18f,
                5,
                0,
                15,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Line,
                10f,
                2.5f,
                CombatDamageType.Physical,
                ResistanceType.Mind,
                VisualEffect.Vfx_Com_Special_Blue_Red,
                VisualEffect.Vfx_Fnf_Screen_Bump)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .MimicryTechnique(FeatType.SuppressingShot, 1, 2)
                .HasTargetingLine(
                    Spell.SuppressingShotTechnique,
                    10f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf | AbilityTargetingFlags.BackOffsetOrigin);

            return _builder.Build();
        }
    }
}
