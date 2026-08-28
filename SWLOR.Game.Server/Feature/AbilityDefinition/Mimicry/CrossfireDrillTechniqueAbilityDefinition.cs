using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class CrossfireDrillTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.CrossfireDrillTechnique,
                "Crossfire Drill",
                Animation.FireForgetDodgeSide,
                InnateAbilityProfile.Mimicry,
                RecastGroup.CrossfireDrill,
                1.0f,
                30f,
                10,
                0,
                30,
                typeof(SuppressionStatusEffect),
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Mind,
                VisualEffect.Vfx_Com_Special_Blue_Red,
                VisualEffect.Vfx_Fnf_Screen_Bump)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Agility)
                .MimicryTechnique(FeatType.CrossfireDrill, 45, 3)
                .HasTargetingCone(
                    Spell.CrossfireDrillTechnique,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf | AbilityTargetingFlags.BackOffsetOrigin);

            return _builder.Build();
        }
    }
}
