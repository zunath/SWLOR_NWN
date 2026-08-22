using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerCircleSurgeTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerCircleSurgeTechnique,
                "Inner Circle Surge",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.InnerCircleSurge,
                1.3f,
                24f,
                9,
                48,
                30,
                typeof(ExposedStatusEffect),
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_M,
                maxRange: 12f,
                damagePercentAdjustment: InnateAbility.ComboBonus(50, typeof(ShockStatusEffect)),
                afterSuccessfulHit: InnateAbility.ChainOnHit(InnateAbilityProfile.Mimicry, 3, 6f, 16, typeof(ShockStatusEffect), 30, CombatDamageType.Electrical))
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.InnerCircleSurge, 45, 3)
                .MimicryElement(CombatDamageType.Electrical);

            return _builder.Build();
        }
    }
}
