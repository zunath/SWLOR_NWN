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
                .MimicryTraitStat(StatType.PhysicalDefensePercentAdjustment, 8)
                .MimicryTraitStat(StatType.ForceDefensePercentAdjustment, 4)
                .MimicryTraitResistance(ResistanceType.Trauma, 15)
                .MimicryTraitResistance(ResistanceType.Fire, 10)
                .MimicryTraitResistance(ResistanceType.Poison, 10);

            return _builder.Build();
        }
    }
}
