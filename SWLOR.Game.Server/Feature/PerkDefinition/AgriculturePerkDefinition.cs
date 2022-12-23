using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class AgriculturePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Synthesis();
            Touch();
            Abilities();
            CookingRecipes();
            AgricultureEquipment();
            FishingRods();

            return _builder.Build();
        }

        private void Synthesis()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.RapidSynthesisCooking)
                .Name("Rapid Synthesis (Cooking)")

                .AddPerkLevel()
                .Description("Increases progress by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 10);


            _builder.Create(PerkCategoryType.Agriculture, PerkType.CarefulSynthesisCooking)
                .Name("Careful Synthesis (Cooking)")

                .AddPerkLevel()
                .Description("Increases progress by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 30);
        }

        private void Touch()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.BasicTouchCooking)
                .Name("Basic Touch (Cooking)")

                .AddPerkLevel()
                .Description("Increases quality by 10. (90% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 5);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.StandardTouchCooking)
                .Name("Standard Touch (Cooking)")

                .AddPerkLevel()
                .Description("Increases quality by 30. (75% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 15);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.PreciseTouchCooking)
                .Name("Precise Touch (Cooking)")

                .AddPerkLevel()
                .Description("Increases quality by 80. (50% success rate)")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 35);
        }

        private void Abilities()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.MastersMendCooking)
                .Name("Master's Mend (Cooking)")

                .AddPerkLevel()
                .Description("Restores item durability by 30.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 10);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.SteadyHandCooking)
                .Name("Steady Hand (Cooking)")

                .AddPerkLevel()
                .Description("Increases success rate of next synthesis ability to 100%.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 20);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.MuscleMemoryCooking)
                .Name("Muscle Memory (Cooking)")

                .AddPerkLevel()
                .Description("Increases success rate of next touch ability to 100%.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 40);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.VenerationCooking)
                .Name("Veneration (Cooking)")

                .AddPerkLevel()
                .Description("Reduces CP cost of Synthesis abilitites by 50% for the next four actions.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 25);

            _builder.Create(PerkCategoryType.Agriculture, PerkType.WasteNotCooking)
                .Name("Waste Not (Cooking)")

                .AddPerkLevel()
                .Description("Reduces loss of durability by 50% for the next four actions.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 8);
        }

        private void CookingRecipes()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.CookingRecipes)
                .Name("Cooking Recipes")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Cooking recipes.")
                .Price(1)
                .GrantsFeat(FeatType.CookingRecipes1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Cooking recipes.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 10)
                .GrantsFeat(FeatType.CookingRecipes2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Cooking recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 20)
                .GrantsFeat(FeatType.CookingRecipes3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Cooking recipes.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 30)
                .GrantsFeat(FeatType.CookingRecipes4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Cooking recipes.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 40)
                .GrantsFeat(FeatType.CookingRecipes5);
        }

        private void AgricultureEquipment()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.AgricultureEquipment)
                .Name("Agriculture Equipment")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Agriculture equipment.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 5)
                .GrantsFeat(FeatType.AgricultureEquipment1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Agriculture equipment.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 15)
                .GrantsFeat(FeatType.AgricultureEquipment2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Agriculture equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 25)
                .GrantsFeat(FeatType.AgricultureEquipment3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Agriculture equipment.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 35)
                .GrantsFeat(FeatType.AgricultureEquipment4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Agriculture equipment.")
                .Price(5)
                .RequirementSkill(SkillType.Agriculture, 45)
                .GrantsFeat(FeatType.AgricultureEquipment5);
        }

        private void FishingRods()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.FishingRods)
                .Name("Fishing Rods")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Fishing Rods.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Fishing Rods.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Fishing Rods.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Fishing Rods.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Fishing Rods.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 40);
        }
    }
}
