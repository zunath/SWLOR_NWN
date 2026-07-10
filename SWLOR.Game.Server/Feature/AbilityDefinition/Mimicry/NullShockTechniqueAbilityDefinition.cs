using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class NullShockTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.NullShockTechnique,
                "Null Shock",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.NullShock,
                1.5f,
                24f,
                8,
                20,
                12,
                typeof(ForceSuppressionStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Force,
                ResistanceType.Disruption,
                VisualEffect.Vfx_Imp_Aura_Negative_Energy,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Evil,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .MimicryTechnique(FeatType.NullShock, 3, 3)
                .HasTargetingSphere(
                    Spell.NullShockTechnique,
                    4.5f,
                    AbilityTargetingFlags.HarmsEnemies);

            return _builder.Build();
        }
    }
}
