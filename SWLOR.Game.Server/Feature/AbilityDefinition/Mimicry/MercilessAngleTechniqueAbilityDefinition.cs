using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class MercilessAngleTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.MercilessAngleTechnique,
                "Merciless Angle",
                Animation.DoubleThrust,
                InnateAbilityProfile.Mimicry,
                RecastGroup.MercilessAngle,
                1.0f,
                30f,
                10,
                40,
                0,
                null,
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 5f,
                damagePercentAdjustment: InnateAbility.ComboBonus(50, typeof(BleedStatusEffect), typeof(HemorrhageStatusEffect)),
                afterSuccessfulHit: ResolveHemorrhage)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.MercilessAngle, 44, 3)
                .HasTargetingCone(
                    Spell.MercilessAngleTechnique,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }

        private static void ResolveHemorrhage(uint activator, uint target)
        {
            if (StatusEffect.HasStatusEffect(target, typeof(BleedStatusEffect), typeof(HemorrhageStatusEffect)))
            {
                InnateAbility.DetonateOnHit(
                    InnateAbilityProfile.Mimicry,
                    40,
                    CombatDamageType.Physical,
                    typeof(BleedStatusEffect),
                    typeof(HemorrhageStatusEffect))(activator, target);
                return;
            }

            StatusEffect.ApplyStatusEffect<HemorrhageStatusEffect>(activator, target, 30f, CombatDamageType.Physical);
        }
    }
}
