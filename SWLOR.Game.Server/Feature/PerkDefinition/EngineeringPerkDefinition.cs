using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class EngineeringPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DroidAssembly();

            return _builder.Build();
        }

        private void DroidAssembly()
        {
            _builder.Create(PerkCategoryType.Engineering, PerkType.DroidAssembly)
                .Name("Droid Assembly")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DroidAssemblyTrait)
                .Description("Enables the construction and programming of tier 1 droids.")
                .Price(1)

                .AddPerkLevel()
                .Description("Enables the construction and programming of tier 2 droids.")
                .Price(1)
                .RequirementSkill(SkillType.Engineering, 10)

                .AddPerkLevel()
                .Description("Enables the construction and programming of tier 3 droids.")
                .Price(2)
                .RequirementSkill(SkillType.Engineering, 20)

                .AddPerkLevel()
                .Description("Enables the construction and programming of tier 4 droids.")
                .Price(3)
                .RequirementSkill(SkillType.Engineering, 30)

                .AddPerkLevel()
                .Description("Enables the construction and programming of tier 5 droids.")
                .Price(4)
                .RequirementSkill(SkillType.Engineering, 40);
        }
    }
}
