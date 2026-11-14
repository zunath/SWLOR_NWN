using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ChitinGuardTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.ChitinGuardTechnique, profile.PlayerPerkType)
                .Name("Chitin Guard")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.ChitinGuard, 20, 2)
                .MimicryTraitStat(StatType.PhysicalDefensePercentAdjustment, 4)
                .MimicryTraitStat(StatType.ForceDefensePercentAdjustment, 8)
                .MimicryTraitResistance(ResistanceType.Fire, 10)
                .MimicryTraitResistance(ResistanceType.Poison, 10);

            return _builder.Build();
        }
    }
}
