using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class IronCarapaceTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.IronCarapaceTechnique, profile.PlayerPerkType)
                .Name("Iron Carapace")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .MimicryTrait(FeatType.IronCarapace, 21, 2)
                .MimicryTraitStat(StatType.PhysicalDefensePercentAdjustment, 15)
                .MimicryTraitStat(StatType.ForceDefensePercentAdjustment, 10)
                .MimicryTraitResistance(ResistanceType.Trauma, 25)
                .MimicryTraitResistance(ResistanceType.Fire, 15)
                .MimicryTraitResistance(ResistanceType.Poison, 15);

            return _builder.Build();
        }
    }
}
