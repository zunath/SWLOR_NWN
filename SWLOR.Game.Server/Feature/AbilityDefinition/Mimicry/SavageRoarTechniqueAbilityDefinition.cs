using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class SavageRoarTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.SavageRoarTechnique,
                "Savage Roar Technique",
                Animation.FireForgetTaunt,
                InnateAbilityProfile.Mimicry,
                RecastGroup.SavageRoar,
                1.0f,
                21f,
                5,
                0,
                14,
                typeof(WeakenedStatusEffect),
                CombatImpactAreaShape.Sphere,
                6f,
                0f,
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Fnf_Howl_War_Cry,
                VisualEffect.Vfx_Fnf_Howl_War_Cry,
                centerOnActivator: true)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.SavageRoar, 2, 2)
                .HasTargetingSphere(
                    Spell.SavageRoarTechnique,
                    6f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
