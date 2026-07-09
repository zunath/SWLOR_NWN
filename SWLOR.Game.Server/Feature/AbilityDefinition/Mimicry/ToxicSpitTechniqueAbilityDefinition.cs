using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ToxicSpitTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.ToxicSpitTechnique,
                "Toxic Spit Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.ToxicSpit,
                1.0f,
                18f,
                3,
                8,
                30,
                typeof(PoisonStatusEffect),
                CombatDamageType.Poison,
                ResistanceType.Poison,
                VisualEffect.Vfx_Imp_Poison_S,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTechnique(FeatType.ToxicSpit, 1, 1);

            return _builder.Build();
        }
    }
}
