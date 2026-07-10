using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class SignalSnareTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.SignalSnareTechnique,
                "Signal Snare Technique",
                Animation.FireForgetTaunt,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                0.8f,
                22f,
                7,
                24,
                6,
                typeof(DisorientedStatusEffect),
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Dazed_S,
                maxRange: 12f)
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.SignalSnare, 3, 2);

            return _builder.Build();
        }
    }
}
