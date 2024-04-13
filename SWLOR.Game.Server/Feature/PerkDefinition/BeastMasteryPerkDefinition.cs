﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastMasteryPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Tame();
            Reward();
            Stabling();
            Snarl();
            Growl();
            SoothePet();
            ReviveBeast();

            DNAManipulation();
            IncubationProcessing();
            ErraticGenius();
            IncubationManagement();

            return _builder.Build();
        }

        private void Tame()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Tame)
                .Name("Tame")

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 0 and 10. Also grants 'Call Beast' ability.")
                .Price(3)
                .GrantsFeat(FeatType.Tame)
                .GrantsFeat(FeatType.CallBeast)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 0 and 20.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 10)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 0 and 30.")
                .Price(4)
                .RequirementSkill(SkillType.BeastMastery, 20)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 0 and 40.")
                .Price(5)
                .RequirementSkill(SkillType.BeastMastery, 30)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 0 and 50.")
                .Price(5)
                .RequirementSkill(SkillType.BeastMastery, 40);
        }

        private void Reward()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Reward)
                .Name("Reward")

                .AddPerkLevel()
                .Description("Restores 50 HP to your pet. Consumes a treat item on use.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 5)
                .GrantsFeat(FeatType.Reward1)

                .AddPerkLevel()
                .Description("Restores 90 HP to your pet. Consumes a treat item on use.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)
                .GrantsFeat(FeatType.Reward2)

                .AddPerkLevel()
                .Description("Restores 130 HP to your pet. Consumes a treat item on use.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 45)
                .GrantsFeat(FeatType.Reward3);
        }

        private void Stabling()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Stabling)
                .Name("Stabling")

                .AddPerkLevel()
                .Description("Permits you to store up to two beasts at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 10)

                .AddPerkLevel()
                .Description("Permits you to store up to three beasts at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 20)

                .AddPerkLevel()
                .Description("Permits you to store up to four beasts at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 30)

                .AddPerkLevel()
                .Description("Permits you to store up to five beasts at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 40)

                .AddPerkLevel()
                .Description("Permits you to store up to six beasts at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 50);
        }
        
        private void Snarl()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Snarl)
                .Name("Snarl")

                .AddPerkLevel()
                .Description("Transfers 50% of your enmity to your pet.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 15)
                .GrantsFeat(FeatType.Snarl);
        }

        private void Growl()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Growl)
                .Name("Growl")

                .AddPerkLevel()
                .Description("Transfers 50% of your pet's enmity to you.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 15)
                .GrantsFeat(FeatType.Growl);
        }

        private void SoothePet()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.SoothePet)
                .Name("Soothe Pet")

                .AddPerkLevel()
                .Description("Removes debuffs from your pet.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)
                .GrantsFeat(FeatType.SoothePet);
        }

        private void ReviveBeast()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.ReviveBeast)
                .Name("Revive Beast")

                .AddPerkLevel()
                .Description("Revives your pet with 1 HP.")
                .Price(1)
                .GrantsFeat(FeatType.ReviveBeast1)

                .AddPerkLevel()
                .Description("Revives your pet with (10+SOC)% HP.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)
                .GrantsFeat(FeatType.ReviveBeast2)

                .AddPerkLevel()
                .Description("Revives your pet with (30+SOC*2)% HP.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 40)
                .GrantsFeat(FeatType.ReviveBeast3);
        }

        private void DNAManipulation()
        {
            _builder.Create(PerkCategoryType.BeastMasteryIncubation, PerkType.DNAManipulation)
                .Name("DNA Manipulation")

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 0 and 10 and use incubators.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 5)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 0 and 20.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 15)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 0 and 30.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 0 and 40.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 35)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 0 and 50.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 45);

        }

        private void IncubationProcessing()
        {
            _builder.Create(PerkCategoryType.BeastMasteryIncubation, PerkType.IncubationProcessing)
                .Name("Incubation Processing")

                .AddPerkLevel()
                .Description("Reduces incubation time by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 15)

                .AddPerkLevel()
                .Description("Reduces incubation time by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)

                .AddPerkLevel()
                .Description("Reduces incubation time by 30%.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 35)

                .AddPerkLevel()
                .Description("Reduces incubation time by 40%.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 45);
        }

        private void ErraticGenius()
        {
            _builder.Create(PerkCategoryType.BeastMasteryIncubation, PerkType.ErraticGenius)
                .Name("Erratic Genius")

                .AddPerkLevel()
                .Description("Increases the mutation chance by 2%.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 20)

                .AddPerkLevel()
                .Description("Increases the mutation chance by 4%.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 30)

                .AddPerkLevel()
                .Description("Increases the mutation chance by 8%.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 40);
        }

        private void IncubationManagement()
        {
            _builder.Create(PerkCategoryType.BeastMasteryIncubation, PerkType.IncubationManagement)
                .Name("Incubation Management")

                .AddPerkLevel()
                .Description("Increases the maximum number of concurrent incubation jobs by 1, for a total of 2.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)

                .AddPerkLevel()
                .Description("Increases the maximum number of concurrent incubation jobs by 1, for a total of 3.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 50);
        }

    }
}
