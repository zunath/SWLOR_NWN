using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
                .Name("Harvesting")

                .AddPerkLevel()
                .Description("You can use tier 1 harvesters.")
                .Price(1)
                .GrantsFeat(FeatType.Harvesting1)

                .AddPerkLevel()
                .Description("You can use tier 2 harvesters.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(FeatType.Harvesting2)

                .AddPerkLevel()
                .Description("You can use tier 3 harvesters.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(FeatType.Harvesting3)

                .AddPerkLevel()
                .Description("You can use tier 4 harvesters.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(FeatType.Harvesting4)

                .AddPerkLevel()
                .Description("You can use tier 5 harvesters.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(FeatType.Harvesting5);
        }

        private void Refining(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.Refining)
                .Name("Refining")

                .AddPerkLevel()
                .Description("You can refine Veldite.")
                .Price(1)
                .GrantsFeat(FeatType.Refining1)

                .AddPerkLevel()
                .Description("You can refine Veldite and Scordspar.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(FeatType.Refining2)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, and Plagionite.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(FeatType.Refining3)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, Plagionite, and Keromber.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(FeatType.Refining4)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, Plagionite, Keromber, and Jasioclase.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(FeatType.Refining5);
        }

        private void RefineryManagement(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.RefineryManagement)
                .Name("Refinery Management")

                .AddPerkLevel()
                .Description("Power sources last 12 seconds longer inside refineries.")
                .Price(1)
                .GrantsFeat(FeatType.RefineryManagement1)

                .AddPerkLevel()
                .Description("Power sources last 18 seconds longer inside refineries.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(FeatType.RefineryManagement2)

                .AddPerkLevel()
                .Description("Power sources last 24 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(FeatType.RefineryManagement3)

                .AddPerkLevel()
                .Description("Power sources last 36 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(FeatType.RefineryManagement4)

                .AddPerkLevel()
                .Description("Power sources last 48 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(FeatType.RefineryManagement5)

                .AddPerkLevel()
                .Description("Power sources last 60 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 45)
                .GrantsFeat(FeatType.RefineryManagement6);
        }

        private void Scavenging(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.Scavenging)
                .Name("Scavenging")

                .AddPerkLevel()
                .Description("You can scavenge tier 1 resources.")
                .Price(1)
                .GrantsFeat(FeatType.Scavenging1)

                .AddPerkLevel()
                .Description("You can scavenge tier 2 resources.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(FeatType.Scavenging2)

                .AddPerkLevel()
                .Description("You can scavenge tier 3 resources.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(FeatType.Scavenging3)

                .AddPerkLevel()
                .Description("You can scavenge tier 4 resources.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(FeatType.Scavenging4)

                .AddPerkLevel()
                .Description("You can scavenge tier 5 resources.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(FeatType.Scavenging5);
        }

        private void HardLook(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.HardLook)
                .Name("Hard Look")

                .AddPerkLevel()
                .Description("Grants a 10% chance to search a second time at each search site.")
                .Price(1)
                .GrantsFeat(FeatType.HardLook1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to search a second time at each search site.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(FeatType.HardLook2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to search a second time at each search site.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(FeatType.HardLook3)

                .AddPerkLevel()
                .Description("Grants a 40% chance to search a second time at each search site.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(FeatType.HardLook4)

                .AddPerkLevel()
                .Description("Grants a 50% chance to search a second time at each search site.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(FeatType.HardLook5);
        }

    }
}
