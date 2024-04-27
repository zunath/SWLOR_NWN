using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class PilotingPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Starships(builder);
            DefensiveModules(builder);
            OffensiveModules(builder);
            EnergyManagement(builder);
            MiningModules(builder);
            StarshipMining(builder);
            IntuitivePiloting(builder);

            return builder.Build();
        }

        private void Starships(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.Starships)
                .Name("Starships")

                .AddPerkLevel()
                .Description("Enables you to pilot tier 1 starships.")
                .Price(1)
                .GrantsFeat(FeatType.Starships1)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 2 starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.Starships2)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 3 starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.Starships3)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 4 starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(FeatType.Starships4)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 5 starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.Starships5);
        }

        private void DefensiveModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.DefensiveModules)
                .Name("Defensive Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 defensive modules on starships.")
                .Price(1)
                .GrantsFeat(FeatType.DefensiveModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 defensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.DefensiveModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 defensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.DefensiveModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 defensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(FeatType.DefensiveModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 defensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.DefensiveModules5);
        }

        private void OffensiveModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.OffensiveModules)
                .Name("Offensive Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 offensive modules on starships.")
                .Price(1)
                .GrantsFeat(FeatType.OffensiveModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 offensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.OffensiveModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 offensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.OffensiveModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 offensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(FeatType.OffensiveModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 offensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.OffensiveModules5);
        }

        private void EnergyManagement(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.EnergyManagement)
                .Name("Energy Management")

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 20%.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.EnergyManagement1)

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 40%.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.EnergyManagement2);
        }

        private void MiningModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.MiningModules)
                .Name("Mining Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 mining modules on starships.")
                .Price(1)
                .GrantsFeat(FeatType.MiningModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 mining modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.MiningModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 mining modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.MiningModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 mining modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(FeatType.MiningModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 mining modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.MiningModules5);
        }

        private void StarshipMining(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.StarshipMining)
                .Name("Starship Mining")

                .AddPerkLevel()
                .Description("Mining yield improves by 1 unit per cycle.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.StarshipMining1)

                .AddPerkLevel()
                .Description("Mining yield improves by 2 units per cycle.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.StarshipMining2);
        }

        private void IntuitivePiloting(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.IntuitivePiloting)
                .Name("Intuitive Piloting")

                .AddPerkLevel()
                .Description("Allows for Willpower to be used in place of Perception for starship module effectiveness.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.IntuitivePiloting);
        }
    }
}
