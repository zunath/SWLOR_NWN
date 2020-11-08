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
                .Description("You can harvest Veldite.")
                .Price(1)
                .GrantsFeat(Feat.Harvesting1)

                .AddPerkLevel()
                .Description("You can harvest Veldite and Scordspar.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(Feat.Harvesting2)

                .AddPerkLevel()
                .Description("You can harvest Veldite, Scordspar, and Plagionite.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(Feat.Harvesting3)

                .AddPerkLevel()
                .Description("You can harvest Veldite, Scordspar, Plagionite, and Keromber.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(Feat.Harvesting4)

                .AddPerkLevel()
                .Description("You can harvest Veldite, Scordspar, Plagionite, Keromber, and Jasioclase.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(Feat.Harvesting5);
        }

        private void Refining(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.Refining)
                .Name("Refining")

                .AddPerkLevel()
                .Description("You can refine Veldite.")
                .Price(1)
                .GrantsFeat(Feat.Refining1)

                .AddPerkLevel()
                .Description("You can refine Veldite and Scordspar.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(Feat.Refining2)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, and Plagionite.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(Feat.Refining3)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, Plagionite, and Keromber.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(Feat.Refining4)

                .AddPerkLevel()
                .Description("You can refine Veldite, Scordspar, Plagionite, Keromber, and Jasioclase.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(Feat.Refining5);
        }

        private void RefineryManagement(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.RefineryManagement)
                .Name("Refinery Management")

                .AddPerkLevel()
                .Description("Power sources last 12 seconds longer inside refineries.")
                .Price(1)
                .GrantsFeat(Feat.RefineryManagement1)

                .AddPerkLevel()
                .Description("Power sources last 18 seconds longer inside refineries.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(Feat.RefineryManagement2)

                .AddPerkLevel()
                .Description("Power sources last 24 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(Feat.RefineryManagement3)

                .AddPerkLevel()
                .Description("Power sources last 36 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(Feat.RefineryManagement4)

                .AddPerkLevel()
                .Description("Power sources last 48 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(Feat.RefineryManagement5)

                .AddPerkLevel()
                .Description("Power sources last 60 seconds longer inside refineries.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 45)
                .GrantsFeat(Feat.RefineryManagement6);
        }

        private void Scavenging(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.Scavenging)
                .Name("Scavenging")

                .AddPerkLevel()
                .Description("You can scavenge tier 1 resources.")
                .Price(1)
                .GrantsFeat(Feat.Scavenging1)

                .AddPerkLevel()
                .Description("You can scavenge tier 2 resources.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(Feat.Scavenging2)

                .AddPerkLevel()
                .Description("You can scavenge tier 3 resources.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(Feat.Scavenging3)

                .AddPerkLevel()
                .Description("You can scavenge tier 4 resources.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(Feat.Scavenging4)

                .AddPerkLevel()
                .Description("You can scavenge tier 5 resources.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(Feat.Scavenging5);
        }

        private void HardLook(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Gathering, PerkType.HardLook)
                .Name("Scavenging")

                .AddPerkLevel()
                .Description("Grants a 10% chance to search a second time at each search site.")
                .Price(1)
                .GrantsFeat(Feat.HardLook1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to search a second time at each search site.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(Feat.HardLook2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to search a second time at each search site.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(Feat.HardLook3)

                .AddPerkLevel()
                .Description("Grants a 40% chance to search a second time at each search site.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(Feat.HardLook4)

                .AddPerkLevel()
                .Description("Grants a 50% chance to search a second time at each search site.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(Feat.HardLook5);
        }

    }
}
