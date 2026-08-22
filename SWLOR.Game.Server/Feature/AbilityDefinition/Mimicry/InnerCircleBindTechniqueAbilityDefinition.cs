using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerCircleBindTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerCircleBindTechnique,
                "Inner Circle Bind",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.InnerCircleBind,
                1.3f,
                24f,
                9,
                0,
                15,
                typeof(WeaponJam1StatusEffect),
                CombatDamageType.Electrical,
                ResistanceType.Disruption,
                VisualEffect.Vfx_Imp_Lightning_M,
                maxRange: 12f,
                additionalStatusEffects: new[] { typeof(ImmobilizedStatusEffect) },
                afterSuccessfulHit: InnateAbility.InterruptOnHit())
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.InnerCircleBind, 44, 3)
                .MimicryElement(CombatDamageType.Electrical);

            return _builder.Build();
        }
    }
}
