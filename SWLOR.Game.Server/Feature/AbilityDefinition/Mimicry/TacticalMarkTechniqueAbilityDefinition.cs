using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class TacticalMarkTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.TacticalMarkTechnique,
                "Tactical Mark Technique",
                Animation.PointForward,
                InnateAbilityProfile.Mimicry,
                RecastGroup.TacticalMark,
                0.9f,
                15f,
                5,
                16,
                15,
                typeof(ExposeWeakPointStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Imp_Magical_Vision,
                maxRange: 12f)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.TacticalMark, 2, 2);

            return _builder.Build();
        }
    }
}
