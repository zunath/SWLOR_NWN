using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class PilotingPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DefensiveModules();
            EnergyManagement();
            IntuitivePiloting();
            MiningModules();
            OffensiveModules();
            StarshipMining();
            Starships();

            return _builder.Build();
        }


        private void DefensiveModules()
        {
            _builder.Create(PerkCategoryType.Piloting, PerkType.DefensiveModules)
                .Name("Defensive Modules")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DefensiveModulesTrait)
                .Description("Enables you to attach tier 1 defensive modules on starships.")
                .Price(1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 defensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 defensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 defensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 defensive modules on starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40);
        }


        private void EnergyManagement()
        {
            _builder.Create(PerkCategoryType.Piloting, PerkType.EnergyManagement)
                .Name("Energy Management")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnergyManagementTrait)
                .Description("Reduces energy consumption of modules by 20%.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 20)

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 40%.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40);
        }


        private void IntuitivePiloting()
        {
            _builder.Create(PerkCategoryType.Piloting, PerkType.IntuitivePiloting)
                .Name("Intuitive Piloting")

                .AddPerkLevel()
                .GrantsFeat(FeatType.IntuitivePilotingTrait)
                .Description("Allows for Willpower to be used in place of Perception for starship module effectiveness.")
                .Price(3)
                .IncreasesStat(StatType.UseWillpowerForPilotingModuleEffectiveness, 1);
        }


        private void MiningModules()
        {
            _builder.Create(PerkCategoryType.Piloting, PerkType.MiningModules)
                .Name("Mining Modules")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MiningModulesTrait)
                .Description("Enables you to attach tier 1 mining modules on starships.")
                .Price(1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 mining modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 mining modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 mining modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 mining modules on starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40);
        }


        private void OffensiveModules()
        {
            _builder.Create(PerkCategoryType.Piloting, PerkType.OffensiveModules)
                .Name("Offensive Modules")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OffensiveModulesTrait)
                .Description("Enables you to attach tier 1 offensive modules on starships.")
                .Price(1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 offensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 offensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 offensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 offensive modules on starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40);
        }


        private void StarshipMining()
        {
            _builder.Create(PerkCategoryType.Piloting, PerkType.StarshipMining)
                .Name("Starship Mining")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StarshipMiningTrait)
                .Description("Mining yield improves by 1 unit per cycle.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 20)

                .AddPerkLevel()
                .Description("Mining yield improves by 2 units per cycle.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40);
        }


        private void Starships()
        {
            _builder.Create(PerkCategoryType.Piloting, PerkType.Starships)
                .Name("Starships")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StarshipsTrait)
                .Description("Enables you to pilot tier 1 starships.")
                .Price(1)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 2 starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 3 starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 4 starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 5 starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40);
        }
    }
}

