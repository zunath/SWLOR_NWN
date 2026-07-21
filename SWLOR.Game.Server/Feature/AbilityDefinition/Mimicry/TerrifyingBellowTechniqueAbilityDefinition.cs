using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class TerrifyingBellowTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.TerrifyingBellowTechnique,
                "Terrifying Bellow",
                Animation.FireForgetTaunt,
                InnateAbilityProfile.Mimicry,
                RecastGroup.TerrifyingBellow,
                1.0f,
                30f,
                10,
                0,
                15,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                6f,
                0f,
                CombatDamageType.Physical,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                maxRange: 6f,
                centerOnActivator: true,
                afterSuccessfulHit: InnateAbility.InterruptOnHit())
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .MimicryTechnique(FeatType.TerrifyingBellow, 11, 3)
                .HasTargetingSphere(
                    Spell.TerrifyingBellowTechnique,
                    6f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
