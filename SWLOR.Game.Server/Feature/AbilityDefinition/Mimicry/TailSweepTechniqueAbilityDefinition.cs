using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class TailSweepTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.TailSweepTechnique,
                "Tail Sweep Technique",
                Animation.Whirlwind,
                InnateAbilityProfile.Mimicry,
                RecastGroup.TailSweep,
                1.4f,
                18f,
                5,
                13,
                4,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Physical,
                ResistanceType.Mind,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                centerOnActivator: true)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.TailSweep, 2, 2)
                .HasTargetingSphere(
                    Spell.TailSweepTechnique,
                    4.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
