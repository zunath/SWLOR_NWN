using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class FinalEclipseTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.FinalEclipseTechnique,
                "Final Eclipse",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.FinalEclipse,
                1.4f,
                30f,
                10,
                40,
                30,
                typeof(ForceDisruptionStatusEffect),
                CombatImpactAreaShape.Line,
                8f,
                2.5f,
                CombatDamageType.Force,
                ResistanceType.Disruption,
                VisualEffect.Vfx_Imp_Aura_Negative_Energy,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Evil,
                afterSuccessfulHit: InnateAbility.RestoreFPOnHit(5),
                damagePercentAdjustment: InnateAbility.ComboBonus(40, typeof(WeakenedStatusEffect)))
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.FinalEclipse, 50, 3)
                .HasTargetingLine(
                    Spell.FinalEclipseTechnique,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf | AbilityTargetingFlags.BackOffsetOrigin);

            return _builder.Build();
        }
    }
}
