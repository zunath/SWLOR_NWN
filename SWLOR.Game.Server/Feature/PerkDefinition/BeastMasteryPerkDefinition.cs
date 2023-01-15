using System.Collections.Generic;
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
            PetManagement();
            Snarl();
            Growl();
            SoothePet();
            ReviveBeast();

            DNAManipulation();
            IncubationProcessing();
            ErraticGenius();

            return _builder.Build();
        }

        private void Tame()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Tame)
                .Name("Tame")

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 0 and 10.")
                .Price(2)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 11 and 20.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 10)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 21 and 30.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 20)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 31 and 40.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 30)

                .AddPerkLevel()
                .Description("Enables you to tame & train creatures between levels 41 and 50.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 40);
        }

        private void Reward()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Reward)
                .Name("Reward")

                .AddPerkLevel()
                .Description("Restores X HP to your pet. Consumes a treat item on use.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 5)

                .AddPerkLevel()
                .Description("Restores X HP to your pet. Consumes a treat item on use.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)

                .AddPerkLevel()
                .Description("Restores X HP to your pet. Consumes a treat item on use.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 45);
        }

        private void Stabling()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Stabling)
                .Name("Stabling")

                .AddPerkLevel()
                .Description("Permits you to store up to one creature at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 15)

                .AddPerkLevel()
                .Description("Permits you to store up to two creatures at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 30)

                .AddPerkLevel()
                .Description("Permits you to store up to three creatures at a stable.")
                .Price(1)
                .RequirementSkill(SkillType.BeastMastery, 50);
        }

        private void PetManagement()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.PetManagement)
                .Name("Pet Management")

                .AddPerkLevel()
                .Description("You can have two pets active at a time.")
                .Price(4)
                .RequirementSkill(SkillType.BeastMastery, 35)

                .AddPerkLevel()
                .Description("You can have three pets active at a time.")
                .Price(4)
                .RequirementSkill(SkillType.BeastMastery, 50);
        }

        private void Snarl()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Snarl)
                .Name("Snarl")

                .AddPerkLevel()
                .Description("Transfers 50% of your enmity to your pet.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 15);
        }

        private void Growl()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.Growl)
                .Name("Growl")

                .AddPerkLevel()
                .Description("Transfers 50% of your pet's enmity to you.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 15);
        }

        private void SoothePet()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.SoothePet)
                .Name("Soothe Pet")

                .AddPerkLevel()
                .Description("Removes debuffs from your pet.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25);
        }

        private void ReviveBeast()
        {
            _builder.Create(PerkCategoryType.BeastMasteryTraining, PerkType.ReviveBeast)
                .Name("Revive Beast")

                .AddPerkLevel()
                .Description("Revives your pet with 1 HP.")
                .Price(1)

                .AddPerkLevel()
                .Description("Revives your pet with (10+SOC)% HP.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)

                .AddPerkLevel()
                .Description("Revives your pet with (30+SOC*2)% HP.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 40);
        }

        private void DNAManipulation()
        {
            _builder.Create(PerkCategoryType.BeastMasteryIncubation, PerkType.DNAManipulation)
                .Name("DNA Manipulation")

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 0 and 10 and use incubation chambers.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 5)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 11 and 20.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 15)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 21 and 30.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 25)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 31 and 40.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 35)

                .AddPerkLevel()
                .Description("Enables you to harvest DNA from creatures between levels 41 and 50.")
                .Price(2)
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
                .Description("Increases the mutation chance by 5%.")
                .Price(2)
                .RequirementSkill(SkillType.BeastMastery, 20)

                .AddPerkLevel()
                .Description("Increases the mutation chance by 10%.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 30)

                .AddPerkLevel()
                .Description("Increases the mutation chance by 15%.")
                .Price(3)
                .RequirementSkill(SkillType.BeastMastery, 40);
        }

    }
}
