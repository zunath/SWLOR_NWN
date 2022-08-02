using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class GatheringPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            TreasureHunter();
            Creditfinder();
            Harvesting();
            Refining();
            RefineryManagement();
            Scavenging();
            HardLook();

            return _builder.Build();
        }

        private void TreasureHunter()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.TreasureHunter)
                .Name("Treasure Hunter")

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 10.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 15)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 20.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 30.")
                .Price(4)
                .RequirementSkill(SkillType.Gathering, 45);
        }

        private void Creditfinder()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.CreditFinder)
                .Name("Creditfinder")

                .AddPerkLevel()
                .Description("Increases the amount of credits found by 20%.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 15)

                .AddPerkLevel()
                .Description("Increases the amount of credits found by 40%.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)

                .AddPerkLevel()
                .Description("Increases the amount of credits found by 60%.")
                .Price(4)
                .RequirementSkill(SkillType.Gathering, 50);
        }

        private void Harvesting()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.Harvesting)
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

        private void Refining()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.Refining)
                .Name("Refining")

                .AddPerkLevel()
                .Description("You can refine Veldite and Tilarium.")
                .Price(1)
                .GrantsFeat(FeatType.Refining1)

                .AddPerkLevel()
                .Description("You can refine Veldite, Tilarium, Scordspar, and Currian.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(FeatType.Refining2)

                .AddPerkLevel()
                .Description("You can refine Veldite, Tilarium, Scordspar, Currian, Plagionite, and Idalia.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(FeatType.Refining3)

                .AddPerkLevel()
                .Description("You can refine Veldite, Tilarium, Scordspar, Currian, Plagionite, Idailia, Keromber, and Barinium.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(FeatType.Refining4)

                .AddPerkLevel()
                .Description("You can refine Veldite, Tilarium, Scordspar, Currian, Plagionite, Idailia, Keromber, Barinium, Jasioclase, and Gostian.")
                .Price(3)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(FeatType.Refining5);
        }

        private void RefineryManagement()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.RefineryManagement)
                .Name("Refinery Management")

                .AddPerkLevel()
                .Description("Power cores refine one additional item.")
                .Price(1)
                .GrantsFeat(FeatType.RefineryManagement1)

                .AddPerkLevel()
                .Description("Power cores refine two additional items.")
                .Price(1)
                .RequirementSkill(SkillType.Gathering, 10)
                .GrantsFeat(FeatType.RefineryManagement2)

                .AddPerkLevel()
                .Description("Power cores refine three additional items.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 20)
                .GrantsFeat(FeatType.RefineryManagement3)

                .AddPerkLevel()
                .Description("Power cores refine four additional items.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 30)
                .GrantsFeat(FeatType.RefineryManagement4)

                .AddPerkLevel()
                .Description("Power cores refine five additional items.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 40)
                .GrantsFeat(FeatType.RefineryManagement5)

                .AddPerkLevel()
                .Description("Power cores refine six additional items.")
                .Price(2)
                .RequirementSkill(SkillType.Gathering, 45)
                .GrantsFeat(FeatType.RefineryManagement6);
        }

        private void Scavenging()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.Scavenging)
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

        private void HardLook()
        {
            _builder.Create(PerkCategoryType.Gathering, PerkType.HardLook)
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
