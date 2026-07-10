using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ChitinGuardTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSelfBuff(
                _builder,
                FeatType.ChitinGuardTechnique,
                "Chitin Guard",
                Animation.ShieldWall,
                InnateAbilityProfile.Mimicry,
                RecastGroup.ChitinGuard,
                0.8f,
                15f,
                5,
                typeof(IronCarapaceStatusEffect),
                30f,
                VisualEffect.Vfx_Imp_Ac_Bonus)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Vitality)
                .MimicryTechnique(FeatType.ChitinGuard, 2, 2);

            return _builder.Build();
        }
    }
}
