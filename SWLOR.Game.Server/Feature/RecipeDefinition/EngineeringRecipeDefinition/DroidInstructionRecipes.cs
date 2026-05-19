using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidInstructionRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }
        private void Tier1()
        {
            // Frag Grenade I
            _builder.Create(RecipeType.InstructionFragGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_fraggren1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Concussion Grenade I
            _builder.Create(RecipeType.InstructionConcussionGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_concgren1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Ion Grenade I
            _builder.Create(RecipeType.InstructionIonGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_iongren1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Flamethrower I
            _builder.Create(RecipeType.InstructionFlamethrower1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flamethrow1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Provoke I
            _builder.Create(RecipeType.InstructionProvoke1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_provoke1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Med Kit I
            _builder.Create(RecipeType.InstructionMedKit1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Treatment Kit I
            _builder.Create(RecipeType.InstructionTreatmentKit1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_treatkit1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Shielding I
            _builder.Create(RecipeType.InstructionShielding1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shielding1")
                .Level(10)
                .Quantity(1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);
        }
        private void Tier2()
        {
            // Frag Grenade II
            _builder.Create(RecipeType.InstructionFragGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_fraggren2")
                .Level(20)
                .Quantity(1)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Wrist Rocket I
            _builder.Create(RecipeType.InstructionWristRocket1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_wristrck1")
                .Level(20)
                .Quantity(1)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Provoke II
            _builder.Create(RecipeType.InstructionProvoke2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_provoke2")
                .Level(20)
                .Quantity(1)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Med Kit II
            _builder.Create(RecipeType.InstructionMedKit2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit2")
                .Level(20)
                .Quantity(1)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Resuscitation I
            _builder.Create(RecipeType.InstructionResuscitation1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_resusc1")
                .Level(20)
                .Quantity(1)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Treatment Kit II
            _builder.Create(RecipeType.InstructionTreatmentKit2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_treatkit2")
                .Level(20)
                .Quantity(1)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Shielding II
            _builder.Create(RecipeType.InstructionShielding2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shielding2")
                .Level(20)
                .Quantity(1)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);
        }
        private void Tier3()
        {
            // Ion Grenade II
            _builder.Create(RecipeType.InstructionIonGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_iongren2")
                .Level(30)
                .Quantity(1)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Adhesive Grenade I
            _builder.Create(RecipeType.InstructionAdhesiveGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_adhgren1")
                .Level(30)
                .Quantity(1)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Flamethrower II
            _builder.Create(RecipeType.InstructionFlamethrower2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flamethrow2")
                .Level(30)
                .Quantity(1)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Wrist Rocket II
            _builder.Create(RecipeType.InstructionWristRocket2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_wristrck2")
                .Level(30)
                .Quantity(1)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Med Kit III
            _builder.Create(RecipeType.InstructionMedKit3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit3")
                .Level(30)
                .Quantity(1)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Infusion I
            _builder.Create(RecipeType.InstructionInfusion1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_infusion1")
                .Level(30)
                .Quantity(1)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);
        }
        private void Tier4()
        {
            // Frag Grenade III
            _builder.Create(RecipeType.InstructionFragGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_fraggren3")
                .Level(40)
                .Quantity(1)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Concussion Grenade II
            _builder.Create(RecipeType.InstructionConcussionGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_concgren2")
                .Level(40)
                .Quantity(1)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Adhesive Grenade II
            _builder.Create(RecipeType.InstructionAdhesiveGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_adhgren2")
                .Level(40)
                .Quantity(1)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Med Kit IV
            _builder.Create(RecipeType.InstructionMedKit4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit4")
                .Level(40)
                .Quantity(1)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Resuscitation II
            _builder.Create(RecipeType.InstructionResuscitation2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_resusc2")
                .Level(40)
                .Quantity(1)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Shielding III
            _builder.Create(RecipeType.InstructionShielding3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shielding3")
                .Level(40)
                .Quantity(1)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);
        }
        private void Tier5()
        {
            // Concussion Grenade III
            _builder.Create(RecipeType.InstructionConcussionGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_concgren3")
                .Level(50)
                .Quantity(1)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Flamethrower III
            _builder.Create(RecipeType.InstructionFlamethrower3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flamethrow3")
                .Level(50)
                .Quantity(1)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Wrist Rocket III
            _builder.Create(RecipeType.InstructionWristRocket3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_wristrck3")
                .Level(50)
                .Quantity(1)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Infusion II
            _builder.Create(RecipeType.InstructionInfusion2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_infusion2")
                .Level(50)
                .Quantity(1)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);
        }
    }
}
