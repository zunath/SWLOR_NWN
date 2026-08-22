using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WardenRendTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.WardenRendTechnique,
                "Warden Rend",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.WardenRend,
                1.1f,
                30f,
                10,
                0,
                30,
                typeof(WeakenedStatusEffect),
                CombatImpactAreaShape.Sphere,
                5.5f,
                0f,
                CombatDamageType.Force,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Aura_Negative_Energy,
                VisualEffect.Vfx_Fnf_Howl_Mind,
                centerOnActivator: true,
                afterSuccessfulHit: InnateAbility.RestoreFPOnHit(4),
                enmityBonus: 75)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.WardenRend, 49, 3)
                .HasTargetingSphere(
                    Spell.WardenRendTechnique,
                    5.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
