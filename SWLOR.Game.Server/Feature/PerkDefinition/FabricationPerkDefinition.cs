using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class FabricationPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Research();
            ScientificNetworking();
            ResearchProjects();

            return _builder.Build();
        }

        private void Research()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.Research)
                .Name("Research")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ResearchTrait)
                .Description("Grants ability to research tier 1 blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 10)

                .AddPerkLevel()
                .Description("Grants ability to research tier 2 blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)

                .AddPerkLevel()
                .Description("Grants ability to research tier 3 blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)

                .AddPerkLevel()
                .Description("Grants ability to research tier 4 blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40)

                .AddPerkLevel()
                .Description("Grants ability to research tier 5 blueprints.")
                .Price(4)
                .RequirementSkill(SkillType.Fabrication, 50);
        }

        private void ScientificNetworking()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.ScientificNetworking)
                .Name("Scientific Networking")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ScientificNetworkingTrait)
                .Description("Blueprints are created with one additional licensed run per rank.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 25)

                .AddPerkLevel()
                .Description("Blueprints are created with one additional licensed run per rank.")
                .Price(4)
                .RequirementSkill(SkillType.Fabrication, 50);
        }

        private void ResearchProjects()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.ResearchProjects)
                .Name("Research Projects")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ResearchProjectsTrait)
                .Description("Increases the maximum number of concurrent research jobs by 1, for a total of 2.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 25)

                .AddPerkLevel()
                .Description("Increases the maximum number of concurrent research jobs by 1, for a total of 3.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 50);
        }
    }
}
