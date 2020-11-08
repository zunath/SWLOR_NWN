using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class GatheringPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Harvesting(builder);
            Refining(builder);
            RefineryManagement(builder);
            Scavenging(builder);
            HardLook(builder);

            return builder.Build();
        }

        private void Harvesting(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.Harvesting)
                .Name("Gathering")

                .AddPerkLevel()
                .Description("You can harvest Veldite.")
                .Price(1)

                .AddPerkLevel()
                .Description("You can harvest Veldite and Scordspar.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)

                .AddPerkLevel()
                .Description("You can harvest Veldite, Scordspar, and Plagionite.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)

                .AddPerkLevel()
                .Description("You can harvest Veldite, Scordspar, Plagionite, and Keromber.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)

                .AddPerkLevel()
                .Description("You can harvest Veldite, Scordspar, Plagionite, Keromber, and Jasioclase.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40);
        }

        private void Refining(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.Refining)
                .Name("Refining")

                .AddPerkLevel()
                .Description("You can refine Veldite.")
                .Price(1)

                .AddPerkLevel()
                .Description("You can refine Veldite and Scordspar.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, and Plagionite.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, Plagionite, and Keromber.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, Plagionite, Keromber, and Jasioclase.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40);
        }

        private void RefineryManagement(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.RefineryManagement)
                .Name("Refinery Management")

                .AddPerkLevel()
                .Description("Power sources last 12 seconds longer inside refineries.")
                .Price(1)

                .AddPerkLevel()
                .Description("Power sources last 18 seconds longer inside refineries.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)

                .AddPerkLevel()
                .Description("Power sources last 24 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)

                .AddPerkLevel()
                .Description("Power sources last 36 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 30)

                .AddPerkLevel()
                .Description("Power sources last 48 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 40)

                .AddPerkLevel()
                .Description("Power sources last 60 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 45);
        }

        private void Scavenging(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.Scavenging)
                .Name("Scavenging")

                .AddPerkLevel()
                .Description("You can scavenge tier 1 resources.")
                .Price(1)

                .AddPerkLevel()
                .Description("You can scavenge tier 2 resources.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)

                .AddPerkLevel()
                .Description("You can scavenge tier 3 resources.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)

                .AddPerkLevel()
                .Description("You can scavenge tier 4 resources.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)

                .AddPerkLevel()
                .Description("You can scavenge tier 5 resources.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40);
        }

        private void HardLook(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.HardLook)
                .Name("Scavenging")

                .AddPerkLevel()
                .Description("Grants a 10% chance to search a second time at each search site.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to search a second time at each search site.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)

                .AddPerkLevel()
                .Description("Grants a 30% chance to search a second time at each search site.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)

                .AddPerkLevel()
                .Description("Grants a 40% chance to search a second time at each search site.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)

                .AddPerkLevel()
                .Description("Grants a 50% chance to search a second time at each search site.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40);
        }

    }
}
