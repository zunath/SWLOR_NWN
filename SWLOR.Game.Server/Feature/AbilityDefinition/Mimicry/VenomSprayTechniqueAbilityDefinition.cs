using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class VenomSprayTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.VenomSprayTechnique,
                "Venom Spray Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.VenomSpray,
                1.4f,
                18f,
                5,
                14,
                12,
                typeof(PoisonStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Poison,
                ResistanceType.Poison,
                VisualEffect.Vfx_Imp_Poison_S,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Acid,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .MimicryTechnique(FeatType.VenomSpray, 2, 2)
                .HasTargetingCone(
                    Spell.VenomSprayTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
