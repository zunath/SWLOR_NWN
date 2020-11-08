using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

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

            return builder.Build();
        }

        private void Starships(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.Starships)
                .Name("Starships")

                .AddPerkLevel()
                .Description("Enables you to pilot basic starships.")
                .Price(1)
                .GrantsFeat(Feat.Starships1)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 1 starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(Feat.Starships2)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 2 starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(Feat.Starships3)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 3 starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(Feat.Starships4)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 4 starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(Feat.Starships5);
        }

        private void DefensiveModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.DefensiveModules)
                .Name("Defensive Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 defensive modules on starships.")
                .Price(1)
                .GrantsFeat(Feat.DefensiveModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 defensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(Feat.DefensiveModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 defensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(Feat.DefensiveModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 defensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(Feat.DefensiveModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 defensive modules on starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(Feat.DefensiveModules5);
        }

        private void OffensiveModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.OffensiveModules)
                .Name("Offensive Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 offensive modules on starships.")
                .Price(1)
                .GrantsFeat(Feat.OffensiveModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 offensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(Feat.OffensiveModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 offensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(Feat.OffensiveModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 offensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(Feat.OffensiveModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 offensive modules on starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(Feat.OffensiveModules5);
        }

        private void EnergyManagement(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.EnergyManagement)
                .Name("Energy Management")

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 25)
                .GrantsFeat(Feat.EnergyManagement1)

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 20%.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 35)
                .GrantsFeat(Feat.EnergyManagement2)

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 30%.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 45)
                .GrantsFeat(Feat.EnergyManagement3);
        }

        private void MiningModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.MiningModules)
                .Name("Mining Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 mining modules on starships.")
                .Price(1)
                .GrantsFeat(Feat.MiningModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 mining modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(Feat.MiningModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 mining modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(Feat.MiningModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 mining modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(Feat.MiningModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 mining modules on starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(Feat.MiningModules5);
        }

        private void StarshipMining(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.StarshipMining)
                .Name("Starship Mining")

                .AddPerkLevel()
                .Description("Mining yield improves by 1 unit per cycle.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(Feat.StarshipMining1)

                .AddPerkLevel()
                .Description("Mining yield improves by 2 units per cycle.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(Feat.StarshipMining2)

                .AddPerkLevel()
                .Description("Mining yield improves by 3 units per cycle.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(Feat.StarshipMining3);
        }

    }
}
