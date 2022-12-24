using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class OneHandedRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Knifes();
            Longswords();
            Lightsabers();

            return _builder.Build();
        }

        private void Knifes()
        {
            // Basic Knife
            _builder.Create(RecipeType.BasicKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("b_knife")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 1)
                .Component("wood", 1);

            // Titan Knife
            _builder.Create(RecipeType.TitanKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("tit_knife")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 1)
                .Component("fine_wood", 1);

            // Sith Knife
            _builder.Create(RecipeType.SithKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("sith_knife")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Delta Knife
            _builder.Create(RecipeType.DeltaKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("del_knife")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 1)
                .Component("ancient_wood", 1);

            // Proto Knife
            _builder.Create(RecipeType.ProtoKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("proto_knife")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 1)
                .Component("aracia_wood", 1);

            // Ophidian Knife
            _builder.Create(RecipeType.OphidianKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("oph_knife")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 1)
                .Component("hyphae_wood", 1);

            // Chiro Knife
            _builder.Create(RecipeType.ChiroKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("chi_knife")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }

        private void Longswords()
        {
            // Basic Longsword
            _builder.Create(RecipeType.BasicLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("b_longsword")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2);

            // Titan Longsword
            _builder.Create(RecipeType.TitanLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("tit_longsword")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Sith Longsword
            _builder.Create(RecipeType.SithLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("sith_longsword")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3);

            // Delta Longsword
            _builder.Create(RecipeType.DeltaLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("del_longsword")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Proto Longsword
            _builder.Create(RecipeType.ProtoLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("pro_longsword")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Ophidian Longsword
            _builder.Create(RecipeType.OphidianLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("oph_longsword")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Chiro Longsword
            _builder.Create(RecipeType.ChiroLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("chi_longsword")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }

        private void Lightsabers()
        {
            // Electroblade I
            _builder.Create(RecipeType.Electroblade1, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 4)
                .Component("ref_veldite", 2);

            // Electroblade II
            _builder.Create(RecipeType.Electroblade2, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_flawed", 4)
                .Component("ref_scordspar", 2);

            // Sith Electroblade
            _builder.Create(RecipeType.SithElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("sith_electro")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_flawed", 5)
                .Component("ref_scordspar", 3);

            // Electroblade III
            _builder.Create(RecipeType.Electroblade3, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 4)
                .Component("ref_plagionite", 2);

            // Electroblade IV
            _builder.Create(RecipeType.Electroblade4, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 4)
                .Component("ref_keromber", 2);

            // Electroblade V
            _builder.Create(RecipeType.Electroblade5, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_high", 4)
                .Component("ref_jasioclase", 2);

            // Chiro Electroblade
            _builder.Create(RecipeType.ChiroElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("chi_electroblade")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Training Saber I
            _builder.Create(RecipeType.TrainingSaber1, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("jade", 4)
                .Component("ref_veldite", 2)
                .Component("elec_ruined", 3);

            // Training Saber II
            _builder.Create(RecipeType.TrainingSaber2, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("agate", 4)
                .Component("ref_scordspar", 2)
                .Component("elec_flawed", 3);

            // Training Saber III
            _builder.Create(RecipeType.TrainingSaber3, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("citrine", 4)
                .Component("ref_plagionite", 2)
                .Component("elec_good", 3);

            // Training Saber IV
            _builder.Create(RecipeType.TrainingSaber4, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ruby", 4)
                .Component("ref_keromber", 2)
                .Component("elec_imperfect", 3);

            // Training Saber V
            _builder.Create(RecipeType.TrainingSaber5, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("emerald", 4)
                .Component("ref_jasioclase", 2)
                .Component("elec_high", 3);

            // Lightsaber Upgrade I
            _builder.Create(RecipeType.LightsaberUpgradeKit1, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_upg1")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.None, 0)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }

    }
}