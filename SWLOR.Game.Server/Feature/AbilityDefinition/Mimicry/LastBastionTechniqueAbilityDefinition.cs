using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class LastBastionTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.LastBastionTechnique,
                "Last Bastion Technique",
                Animation.ShieldWall,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.4f,
                38f,
                9,
                29,
                14,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Line,
                8f,
                2.5f,
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                VisualEffect.Vfx_Fnf_Screen_Shake,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .MimicryTechnique(FeatType.LastBastion, 4, 3)
                .HasTargetingLine(
                    Spell.LastBastionTechnique,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
