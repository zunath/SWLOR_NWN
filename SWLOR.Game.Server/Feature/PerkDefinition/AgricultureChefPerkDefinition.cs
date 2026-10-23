using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Chef line of Agriculture: ten purchases at Agriculture 5/10/15/20/25/30/35/40/45/50 for 1/1/2/2/3/3/3/3/3/4 SP, each a
    /// stat adjustment the Kitchen reads. The rank-20 purchase is the style, a choice of two through
    /// <see cref="PerkBuilder.RequirementCannotHavePerk"/>, and the owned style is always active. Seasoned Palate is the one choice
    /// made for each dish: its switch is in the Kitchen's setup view.
    /// </summary>
    public class AgricultureChefPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            PantryStaples();
            SharpPalate();
            SeasonedPalate();
            SlowCooking();
            FlashCooking();
            Reduce();
            AromaticKitchen();
            SteadyWrist();
            SignatureDish();

            return _builder.Build();
        }

        private void PantryStaples()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.PantryStaples)
                .Name("Pantry Staples")
                .Icon("ife_preparation")

                .AddPerkLevel()
                .Description("In the Kitchen, each free pantry pinch (Salt, Sugar and Spice) pushes 5 instead of 4.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 5)
                .IncreasesStat(StatType.KitchenPantryPush, 1);
        }

        private void SharpPalate()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.SharpPalate)
                .Name("Sharp Palate")
                .Icon("ife_infusion")

                .AddPerkLevel()
                .Description("In the Kitchen, every push lands closer to its listed strength: the base spread is 45% instead of 50%, before Control narrows it.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 10)
                .IncreasesStat(StatType.KitchenPushSpreadReduction, 5);
        }

        private void SeasonedPalate()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.SeasonedPalate)
                .Name("Seasoned Palate")
                .Icon("ife_Culinarian")

                .AddPerkLevel()
                .Description("Adds a Seasoned Palate switch to the Kitchen's setup view. While it is on, every Add of a committed ingredient pushes half as far but still uses a whole portion. Pantry pinches are not changed.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 15)
                .IncreasesStat(StatType.KitchenSeasonedPalate, 1)

                .AddPerkLevel()
                .Description("While Seasoned Palate is on, every push is also 25% less random, after Control narrows it. Everything Seasoned Palate I does still applies.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 30)
                .IncreasesStat(StatType.KitchenSeasonedPalate, 1)
                .IncreasesStat(StatType.KitchenSeasonedPalateSpreadPercent, 25);
        }

        private void SlowCooking()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.SlowCooking)
                .Name("Slow Cooking")
                .Icon("ife_coolfire")

                .AddPerkLevel()
                .Description("Your Kitchen style, always active: Simmer at Low heat gives twice the Doneness (10 instead of 5), and the flavor drifts 25% less at Medium heat. Cannot be purchased with Flash Cooking.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 20)
                .RequirementCannotHavePerk(PerkType.FlashCooking)
                .IncreasesStat(StatType.KitchenLowHeatDonenessPercent, 100)
                .IncreasesStat(StatType.KitchenMediumDriftReductionPercent, 25);
        }

        private void FlashCooking()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.FlashCooking)
                .Name("Flash Cooking")
                .Icon("ife_firestarter")

                .AddPerkLevel()
                .Description("Your Kitchen style, always active: the flavor drifts 20% less at High heat. Cannot be purchased with Slow Cooking.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 20)
                .RequirementCannotHavePerk(PerkType.SlowCooking)
                .IncreasesStat(StatType.KitchenHighHeatDriftReductionPercent, 20);
        }

        private void Reduce()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.KitchenReduce)
                .Name("Reduce")
                .Icon("ife_Alchemist")

                .AddPerkLevel()
                .Description("Adds the Reduce action to the Kitchen. It takes a turn and 12 CP, and pulls the flavor 20% of the way toward the target's center on every flavor.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 25)
                .IncreasesStat(StatType.KitchenReducePercent, 20)

                .AddPerkLevel()
                .Description("Reduce pulls the flavor 35% of the way toward the target's center and costs 9 CP.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 40)
                .IncreasesStat(StatType.KitchenReducePercent, 35)
                .IncreasesStat(StatType.KitchenReduceCpReduction, 3);
        }

        private void AromaticKitchen()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.AromaticKitchen)
                .Name("Aromatic Kitchen")
                .Icon("ife_smoke_screen")

                .AddPerkLevel()
                .Description("In the Kitchen, each turn has a 35% chance of announcing Peak Aroma instead of 25%.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 35)
                .IncreasesStat(StatType.KitchenPeakAromaChancePoints, 10);
        }

        private void SteadyWrist()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.SteadyWrist)
                .Name("Steady Wrist")
                .Icon("ife_steady_hand")

                .AddPerkLevel()
                .Description("In the Kitchen, Stir costs 4 less CP (8 instead of 12) and, at Medium heat, also gives half of a Simmer's Doneness.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 45)
                .IncreasesStat(StatType.KitchenStirCpReduction, 4)
                .IncreasesStat(StatType.KitchenStirDonenessPercent, 50);
        }

        private void SignatureDish()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.SignatureDish)
                .Name("Signature Dish")
                .Icon("ife_carve_and_sp")

                .AddPerkLevel()
                .Description("Adds Signature Dish to the Kitchen: once per dish, it takes a turn and no CP and sets one flavor of your choice exactly to the target's center.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 50)
                .IncreasesStat(StatType.KitchenSignatureDish, 1);
        }
    }
}
