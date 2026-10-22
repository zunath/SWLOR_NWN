using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Builder line of Fabrication: ten purchases at Fabrication 5/10/15/20/25/30/35/40/45/50 for 1/1/2/2/3/3/3/3/3/4 SP, each
    /// a stat adjustment the Fabricator reads. The rank-20 purchase is the style, a choice of two through
    /// <see cref="PerkBuilder.RequirementCannotHavePerk"/>, and the owned style is always active.
    /// </summary>
    public class FabricationBuilderPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            QuickChange();
            HardenedHeads();
            FieldService();
            ProductionRun();
            FineTolerance();
            DualFeed();
            FeedstockReserve();
            FeedstockContract();
            MasterAssembler();

            return _builder.Build();
        }

        private void QuickChange()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.QuickChange)
                .Name("Quick Change")
                .Icon("ife_reassemble")

                .AddPerkLevel()
                .Description("In the Fabricator, Retool costs 25% less CP, to a minimum of 1 CP.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 5)
                .IncreasesStat(StatType.FabricatorRetoolCpReductionPercent, 25)

                .AddPerkLevel()
                .Description("In the Fabricator, Retool costs 50% less CP, to a minimum of 1 CP.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 35)
                .IncreasesStat(StatType.FabricatorRetoolCpReductionPercent, 50);
        }

        private void HardenedHeads()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.HardenedHeads)
                .Name("Hardened Heads")
                .Icon("ife_gearhead")

                .AddPerkLevel()
                .Description("In the Fabricator, Run and Precision Pass wear the machine 1 less Machine Condition, to a minimum of 1. Retool's wear is unchanged.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)
                .IncreasesStat(StatType.FabricatorWearReduction, 1);
        }

        private void FieldService()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.FieldService)
                .Name("Field Service")
                .Icon("ife_repair")

                .AddPerkLevel()
                .Description("In the Fabricator, Service restores 10 more Machine Condition and costs 2 less CP, to a minimum of 1 CP.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 15)
                .IncreasesStat(StatType.FabricatorServiceRestore, 10)
                .IncreasesStat(StatType.FabricatorServiceCpReduction, 2)

                .AddPerkLevel()
                .Description("In the Fabricator, Service restores 20 more Machine Condition and costs 4 less CP, to a minimum of 1 CP.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40)
                .IncreasesStat(StatType.FabricatorServiceRestore, 20)
                .IncreasesStat(StatType.FabricatorServiceCpReduction, 4);
        }

        private void ProductionRun()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.ProductionRun)
                .Name("Production Run")
                .Icon("ife_mastercraft")

                .AddPerkLevel()
                .Description("Your Fabricator style, always active: Run is 25% stronger and Precision Pass 25% weaker. Cannot be purchased with Fine Tolerance.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)
                .RequirementCannotHavePerk(PerkType.FineTolerance)
                .IncreasesStat(StatType.FabricatorRunPowerPercent, 25)
                .IncreasesStat(StatType.FabricatorPrecisionPowerPercent, -25);
        }

        private void FineTolerance()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.FineTolerance)
                .Name("Fine Tolerance")
                .Icon("ife_exmachine")

                .AddPerkLevel()
                .Description("Your Fabricator style, always active: Precision Pass is 25% stronger and Run 25% weaker. Cannot be purchased with Production Run.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)
                .RequirementCannotHavePerk(PerkType.ProductionRun)
                .IncreasesStat(StatType.FabricatorPrecisionPowerPercent, 25)
                .IncreasesStat(StatType.FabricatorRunPowerPercent, -25);
        }

        private void DualFeed()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.DualFeed)
                .Name("Dual Feed")
                .Icon("ife_crafting_log")

                .AddPerkLevel()
                .Description("Adds Dual Feed to the Fabricator: one action builds the loaded head's sub-assembly and one other at 60% of a full Run each. It costs the CP of a Retool and one Run's wear, and the loaded head stays loaded.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 25)
                .IncreasesStat(StatType.FabricatorDualFeedUnlocked, 1);
        }

        private void FeedstockReserve()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.FeedstockReserve)
                .Name("Feedstock Reserve")
                .Icon("ife_steam-powere")

                .AddPerkLevel()
                .Description("In the Fabricator, Prime Feedstock waits 1 more action for its head, so you have more time to retool to it.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)
                .IncreasesStat(StatType.FabricatorPrimeWindowActions, 1);
        }

        private void FeedstockContract()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.FeedstockContract)
                .Name("Feedstock Contract")
                .Icon("ife_treasure_box")

                .AddPerkLevel()
                .Description("In the Fabricator, Prime Feedstock gives its head's next action +75% instead of +50%.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 45)
                .IncreasesStat(StatType.FabricatorPrimeBonusPercent, 25);
        }

        private void MasterAssembler()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.MasterAssembler)
                .Name("Master Assembler")
                .Icon("ife_buildrobot")

                .AddPerkLevel()
                .Description("In the Fabricator, Assemble lets the piece's quality rise to its weakest sub-assembly + 35 instead of + 20.")
                .Price(4)
                .RequirementSkill(SkillType.Fabrication, 50)
                .IncreasesStat(StatType.FabricatorAssembleCapPoints, 15);
        }
    }
}
