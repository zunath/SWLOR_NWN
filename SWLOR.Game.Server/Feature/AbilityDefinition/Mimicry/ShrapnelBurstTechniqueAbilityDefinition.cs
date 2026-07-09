using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ShrapnelBurstTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.ShrapnelBurstTechnique,
                "Shrapnel Burst Technique",
                Animation.ThrowGrenade,
                InnateAbilityProfile.Mimicry,
                RecastGroup.ShrapnelBurst,
                1.4f,
                20f,
                6,
                18,
                12,
                typeof(SunderStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Imp_Wallspike,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .MimicryTechnique(FeatType.ShrapnelBurst, 3, 3)
                .HasTargetingCone(
                    Spell.ShrapnelBurstTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
