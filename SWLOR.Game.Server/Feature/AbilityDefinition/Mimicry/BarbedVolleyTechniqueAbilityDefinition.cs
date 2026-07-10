using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class BarbedVolleyTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.BarbedVolleyTechnique,
                "Barbed Volley Technique",
                Animation.ThrowGrenade,
                InnateAbilityProfile.Mimicry,
                RecastGroup.BarbedVolley,
                1.4f,
                17f,
                5,
                13,
                12,
                typeof(BleedStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Imp_Wallspike,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .MimicryTechnique(FeatType.BarbedVolley, 2, 2)
                .HasTargetingCone(
                    Spell.BarbedVolleyTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
