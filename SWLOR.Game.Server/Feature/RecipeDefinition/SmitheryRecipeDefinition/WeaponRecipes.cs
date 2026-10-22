using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class WeaponRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Knifes();
            IntermediateKnifes();
            Longswords();
            IntermediateLongswords();
            Lightsabers();
            IntermediateLightsabers();
            GreatSwords();
            IntermediateGreatSwords();
            Spears();
            IntermediateSpears();
            TwinBlades();
            IntermediateTwinBlades();
            Saberstaffs();
            IntermediateSaberstaffs();
            Katars();
            IntermediateKatars();
            Staffs();
            IntermediateStaffs();
            Pistols();
            IntermediatePistols();
            Shurikens();
            IntermediateShurikens();
            Rifles();
            IntermediateRifles();

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
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 1)
                .Component("wood", 1)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 55, coolingRate: 1, startingThermal: 86, shapeShare: 44);

            // Titan Knife
            _builder.Create(RecipeType.TitanKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("tit_knife")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 1)
                .Component("fine_wood", 1)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 53, coolingRate: 3, startingThermal: 76, shapeShare: 41);

            // Sith Knife
            _builder.Create(RecipeType.SithKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("sith_knife")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 50, coolingRate: 3, startingThermal: 74, shapeShare: 45);

            // Delta Knife
            _builder.Create(RecipeType.DeltaKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("del_knife")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 1)
                .Component("ancient_wood", 1)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 39, coolingRate: 3, startingThermal: 70, shapeShare: 43);

            // Proto Knife
            _builder.Create(RecipeType.ProtoKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("proto_knife")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 1)
                .Component("aracia_wood", 1)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 37, coolingRate: 3, startingThermal: 69, shapeShare: 42);

            // Ophidian Knife
            _builder.Create(RecipeType.OphidianKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("oph_knife")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 1)
                .Component("hyphae_wood", 1)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 33, coolingRate: 6, startingThermal: 66, shapeShare: 44);

            // Chiro Knife
            _builder.Create(RecipeType.ChiroKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("chi_knife")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 25, coolingRate: 7, startingThermal: 64, shapeShare: 41);
        }

        private void Longswords()
        {
            // Basic Vibroblade LS
            _builder.Create(RecipeType.BasicLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("b_longsword")
                .Level(4)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 55, coolingRate: 2, startingThermal: 83, shapeShare: 45);

            // Titan Longsword
            _builder.Create(RecipeType.TitanLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("tit_longsword")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 46, coolingRate: 3, startingThermal: 73, shapeShare: 41);

            // Sith Longsword
            _builder.Create(RecipeType.SithLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("sith_longsword")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 44, coolingRate: 2, startingThermal: 74, shapeShare: 41);

            // Delta Longsword
            _builder.Create(RecipeType.DeltaLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("del_longsword")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 41, coolingRate: 4, startingThermal: 68, shapeShare: 40);

            // Proto Longsword
            _builder.Create(RecipeType.ProtoLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("pro_longsword")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 34, coolingRate: 3, startingThermal: 67, shapeShare: 39);

            // Ophidian Longsword
            _builder.Create(RecipeType.OphidianLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("oph_longsword")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 29, coolingRate: 4, startingThermal: 63, shapeShare: 44);

            // Chiro Longsword
            _builder.Create(RecipeType.ChiroLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("chi_longsword")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 26, coolingRate: 7, startingThermal: 60, shapeShare: 41);
        }

        private void Lightsabers()
        {
            // Electroblade I
            _builder.Create(RecipeType.Electroblade1, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_1")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 4)
                .Component("ref_veldite", 2)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 57, coolingRate: 3, startingThermal: 75, shapeShare: 35);

            // Electroblade II
            _builder.Create(RecipeType.Electroblade2, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_2")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_flawed", 4)
                .Component("ref_scordspar", 2)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 49, coolingRate: 2, startingThermal: 62, shapeShare: 39);

            // Sith Electroblade
            _builder.Create(RecipeType.SithElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("sith_electro")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_flawed", 5)
                .Component("ref_scordspar", 3)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 43, coolingRate: 2, startingThermal: 63, shapeShare: 39);

            // Electroblade III
            _builder.Create(RecipeType.Electroblade3, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_3")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 4)
                .Component("ref_plagionite", 2)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 40, coolingRate: 4, startingThermal: 59, shapeShare: 36);

            // Electroblade IV
            _builder.Create(RecipeType.Electroblade4, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_4")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 4)
                .Component("ref_keromber", 2)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 33, coolingRate: 4, startingThermal: 55, shapeShare: 35);

            // Electroblade V
            _builder.Create(RecipeType.Electroblade5, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_5")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_high", 4)
                .Component("ref_jasioclase", 2)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 31, coolingRate: 5, startingThermal: 54, shapeShare: 40);

            // Chiro Electroblade
            _builder.Create(RecipeType.ChiroElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("chi_electroblade")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 24, coolingRate: 6, startingThermal: 54, shapeShare: 40);

            // Training Saber I
            _builder.Create(RecipeType.TrainingSaber1, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_1")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("jade", 4)
                .Component("ref_veldite", 2)
                .Component("elec_ruined", 3)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 55, coolingRate: 3, startingThermal: 73, shapeShare: 37);

            // Training Saber II
            _builder.Create(RecipeType.TrainingSaber2, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_2")
                .Level(18)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("agate", 4)
                .Component("ref_scordspar", 2)
                .Component("elec_flawed", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 41, coolingRate: 2, startingThermal: 66, shapeShare: 36);

            // Training Saber III
            _builder.Create(RecipeType.TrainingSaber3, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_3")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("citrine", 4)
                .Component("ref_plagionite", 2)
                .Component("elec_good", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 38, coolingRate: 5, startingThermal: 59, shapeShare: 39);

            // Training Saber IV
            _builder.Create(RecipeType.TrainingSaber4, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_4")
                .Level(38)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ruby", 4)
                .Component("ref_keromber", 2)
                .Component("elec_imperfect", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 36, coolingRate: 6, startingThermal: 55, shapeShare: 35);

            // Training Saber V
            _builder.Create(RecipeType.TrainingSaber5, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_5")
                .Level(48)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("emerald", 4)
                .Component("ref_jasioclase", 2)
                .Component("elec_high", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 29, coolingRate: 6, startingThermal: 55, shapeShare: 41);

        }

        private void GreatSwords()
        {
            // Basic Great Sword
            _builder.Create(RecipeType.BasicGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("b_greatsword")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 53, coolingRate: 2, startingThermal: 81, shapeShare: 42);

            // Titan Great Sword
            _builder.Create(RecipeType.TitanGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("tit_greatsword")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 44, coolingRate: 4, startingThermal: 70, shapeShare: 42);

            // Sith Great Sword
            _builder.Create(RecipeType.SithGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("sith_gswd")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 49, coolingRate: 3, startingThermal: 78, shapeShare: 39);

            // Delta Great Sword
            _builder.Create(RecipeType.DeltaGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("del_greatsword")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 36, coolingRate: 4, startingThermal: 65, shapeShare: 41);

            // Proto Great Sword
            _builder.Create(RecipeType.ProtoGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("proto_greatsword")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 36, coolingRate: 5, startingThermal: 67, shapeShare: 45);

            // Ophidian Great Sword
            _builder.Create(RecipeType.OphidianGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("oph_greatsword")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 3)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 24, coolingRate: 5, startingThermal: 62, shapeShare: 40);

            // Chiro Great Sword
            _builder.Create(RecipeType.ChiroGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("chi_greatsword")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 25, coolingRate: 6, startingThermal: 64, shapeShare: 45);
        }

        private void Spears()
        {
            // Basic Spear
            _builder.Create(RecipeType.BasicSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("b_spear")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 52, coolingRate: 1, startingThermal: 74, shapeShare: 45);

            // Titan Spear
            _builder.Create(RecipeType.TitanSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("tit_spear")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 44, coolingRate: 3, startingThermal: 72, shapeShare: 39);

            // Sith Spear
            _builder.Create(RecipeType.SithSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("sith_spear")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 51, coolingRate: 3, startingThermal: 76, shapeShare: 42);

            // Delta Spear
            _builder.Create(RecipeType.DeltaSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("del_spear")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 40, coolingRate: 4, startingThermal: 64, shapeShare: 45);

            // Proto Spear
            _builder.Create(RecipeType.ProtoSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("proto_spear")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 31, coolingRate: 6, startingThermal: 68, shapeShare: 41);

            // Ophidian Spear
            _builder.Create(RecipeType.OphidianSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("oph_spear")
                .Level(47)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("aracia_wood", 4)
                .Component("hyphae_wood", 2)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 27, coolingRate: 5, startingThermal: 58, shapeShare: 45);

            // Chiro Spear
            _builder.Create(RecipeType.ChiroSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("chi_spear")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 19, coolingRate: 6, startingThermal: 64, shapeShare: 39);

            // Alchemized Spear
            _builder.Create(RecipeType.AlchemizedSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("alc_spear")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("frogguts", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("stolen_s_artifac", 5)
                .Component("emerald", 5)
                .Component("tukata_hide", 5)
                .Component("froglegs", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 24, coolingRate: 6, startingThermal: 63, shapeShare: 41);
        }

        private void TwinBlades()
        {
            // Basic Twin Blade
            _builder.Create(RecipeType.BasicTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("b_twinblade")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 54, coolingRate: 3, startingThermal: 77, shapeShare: 45);

            // Titan Twin Blade
            _builder.Create(RecipeType.TitanTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("tit_twinblade")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 49, coolingRate: 2, startingThermal: 75, shapeShare: 40);

            // Sith Twin Blade
            _builder.Create(RecipeType.SithTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("sith_twinblade")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 45, coolingRate: 3, startingThermal: 72, shapeShare: 39);

            // Delta Twin Blade
            _builder.Create(RecipeType.DeltaTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("del_twinblade")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 38, coolingRate: 5, startingThermal: 70, shapeShare: 39);

            // Proto Twin Blade
            _builder.Create(RecipeType.ProtoTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("proto_twinblade")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 33, coolingRate: 6, startingThermal: 59, shapeShare: 40);

            // Ophidian Twin Blade
            _builder.Create(RecipeType.OphidianTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("oph_twinblade")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 24, coolingRate: 6, startingThermal: 58, shapeShare: 42);

            // Chiro Twin Blade
            _builder.Create(RecipeType.ChiroTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("chi_twinblade")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 20, coolingRate: 6, startingThermal: 58, shapeShare: 43);
        }

        private void Saberstaffs()
        {
            // Twin Electroblade I
            _builder.Create(RecipeType.TwinElectroblade1, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_1")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 5)
                .Component("ref_veldite", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 56, coolingRate: 2, startingThermal: 68, shapeShare: 36);

            // Twin Electroblade II
            _builder.Create(RecipeType.TwinElectroblade2, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_2")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_flawed", 5)
                .Component("ref_scordspar", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 48, coolingRate: 4, startingThermal: 68, shapeShare: 40);

            // Twin Electroblade III
            _builder.Create(RecipeType.TwinElectroblade3, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_3")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 5)
                .Component("ref_plagionite", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 41, coolingRate: 4, startingThermal: 61, shapeShare: 40);

            // Twin Electroblade IV
            _builder.Create(RecipeType.TwinElectroblade4, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_4")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 5)
                .Component("ref_keromber", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 34, coolingRate: 4, startingThermal: 56, shapeShare: 36);

            // Twin Electroblade V
            _builder.Create(RecipeType.TwinElectroblade5, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_5")
                .Level(47)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_high", 5)
                .Component("ref_jasioclase", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 27, coolingRate: 7, startingThermal: 52, shapeShare: 39);

            // Chiro Twin Electroblade
            _builder.Create(RecipeType.ChiroTwinElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("chi_twinelec")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 25, coolingRate: 7, startingThermal: 49, shapeShare: 35);

            // Training Saberstaff I
            _builder.Create(RecipeType.TrainingSaberstaff1, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_1")
                .Level(9)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("jade", 5)
                .Component("ref_veldite", 3)
                .Component("elec_ruined", 4)
                .ForgeParameters(rangeCenter: 53, rangeWidth: 51, coolingRate: 3, startingThermal: 73, shapeShare: 38);

            // Training Saberstaff II
            _builder.Create(RecipeType.TrainingSaberstaff2, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_2")
                .Level(19)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("agate", 5)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 4)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 40, coolingRate: 4, startingThermal: 61, shapeShare: 35);

            // Training Saberstaff III
            _builder.Create(RecipeType.TrainingSaberstaff3, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_3")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("citrine", 5)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 4)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 34, coolingRate: 5, startingThermal: 54, shapeShare: 39);

            // Training Saberstaff IV
            _builder.Create(RecipeType.TrainingSaberstaff4, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_4")
                .Level(39)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ruby", 5)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 4)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 28, coolingRate: 4, startingThermal: 59, shapeShare: 35);

            // Training Saberstaff V
            _builder.Create(RecipeType.TrainingSaberstaff5, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_5")
                .Level(49)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("emerald", 5)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 4)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 21, coolingRate: 6, startingThermal: 52, shapeShare: 40);

        }

        private void Katars()
        {
            // Basic Katar
            _builder.Create(RecipeType.BasicKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("b_katar")
                .Level(3)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 1)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 59, coolingRate: 1, startingThermal: 81, shapeShare: 44);

            // Titan Katar
            _builder.Create(RecipeType.TitanKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("tit_katar")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 50, coolingRate: 1, startingThermal: 70, shapeShare: 44);

            // Sith Katar
            _builder.Create(RecipeType.SithKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("sith_katar")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 47, coolingRate: 4, startingThermal: 72, shapeShare: 44);

            // Delta Katar
            _builder.Create(RecipeType.DeltaKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("del_katar")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 41, coolingRate: 2, startingThermal: 65, shapeShare: 39);

            // Proto Katar
            _builder.Create(RecipeType.ProtoKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("proto_katar")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 37, coolingRate: 5, startingThermal: 62, shapeShare: 42);

            // Ophidian Katar
            _builder.Create(RecipeType.OphidianKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("oph_katar")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 27, coolingRate: 5, startingThermal: 62, shapeShare: 39);

            // Chiro Katar
            _builder.Create(RecipeType.ChiroKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("chi_katar")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 22, coolingRate: 7, startingThermal: 64, shapeShare: 44);
        }

        private void Staffs()
        {
            // Basic Staff
            _builder.Create(RecipeType.BasicStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("b_staff")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 53, coolingRate: 1, startingThermal: 81, shapeShare: 41);

            // Titan Staff
            _builder.Create(RecipeType.TitanStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("tit_staff")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 47, coolingRate: 1, startingThermal: 70, shapeShare: 42);

            // Sith Staff
            _builder.Create(RecipeType.SithStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("sith_staff")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 46, coolingRate: 2, startingThermal: 72, shapeShare: 45);

            // Delta Staff
            _builder.Create(RecipeType.DeltaStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("del_staff")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 41, coolingRate: 3, startingThermal: 69, shapeShare: 41);

            // Proto Staff
            _builder.Create(RecipeType.ProtoStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("proto_staff")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 37, coolingRate: 3, startingThermal: 65, shapeShare: 45);

            // Ophidian Staff
            _builder.Create(RecipeType.OphidianStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("oph_staff")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 25, coolingRate: 6, startingThermal: 65, shapeShare: 45);

            // Chiro Staff
            _builder.Create(RecipeType.ChiroStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("chi_staff")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 19, coolingRate: 5, startingThermal: 62, shapeShare: 43);

        }

        private void Pistols()
        {
            // Basic Pistol
            _builder.Create(RecipeType.BasicPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("b_pistol")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 4)
                .Component("elec_ruined", 2)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 51, coolingRate: 1, startingThermal: 77, shapeShare: 41);

            // Titan Pistol
            _builder.Create(RecipeType.TitanPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("tit_pistol")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 4)
                .Component("elec_flawed", 2)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 48, coolingRate: 1, startingThermal: 66, shapeShare: 37);

            // Sith Pistol
            _builder.Create(RecipeType.SithPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("sith_pistol")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 3)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 43, coolingRate: 4, startingThermal: 65, shapeShare: 37);

            // Delta Pistol
            _builder.Create(RecipeType.DeltaPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("del_pistol")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 4)
                .Component("elec_good", 2)
                .ForgeParameters(rangeCenter: 53, rangeWidth: 37, coolingRate: 4, startingThermal: 62, shapeShare: 37);

            // Proto Pistol
            _builder.Create(RecipeType.ProtoPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("proto_pistol")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 2)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 29, coolingRate: 4, startingThermal: 59, shapeShare: 42);

            // Ophidian Pistol
            _builder.Create(RecipeType.OphidianPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("oph_pistol")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 4)
                .Component("elec_high", 2)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 26, coolingRate: 6, startingThermal: 59, shapeShare: 41);

            // Chiro Pistol
            _builder.Create(RecipeType.ChiroPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("chi_pistol")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 24, coolingRate: 6, startingThermal: 56, shapeShare: 41);
        }

        private void Shurikens()
        {
            // Basic Shuriken
            _builder.Create(RecipeType.BasicShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("b_shuriken")
                .Level(2)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 1)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 59, coolingRate: 1, startingThermal: 83, shapeShare: 42);

            // Titan Shuriken
            _builder.Create(RecipeType.TitanShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("tit_shuriken")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 50, coolingRate: 1, startingThermal: 75, shapeShare: 41);

            // Sith Shuriken
            _builder.Create(RecipeType.SithShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("sith_shuriken")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 46, coolingRate: 2, startingThermal: 75, shapeShare: 43);

            // Delta Shuriken
            _builder.Create(RecipeType.DeltaShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("del_shuriken")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 42, coolingRate: 4, startingThermal: 65, shapeShare: 45);

            // Proto Shuriken
            _builder.Create(RecipeType.ProtoShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("proto_shuriken")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 34, coolingRate: 3, startingThermal: 62, shapeShare: 40);

            // Ophidian Shuriken
            _builder.Create(RecipeType.OphidianShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("oph_shuriken")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 31, coolingRate: 6, startingThermal: 64, shapeShare: 41);

            // Chiro Shuriken
            _builder.Create(RecipeType.ChiroShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("chi_shuriken")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 23, coolingRate: 7, startingThermal: 59, shapeShare: 43);
        }

        private void Rifles()
        {
            // Basic Rifle
            _builder.Create(RecipeType.BasicRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("b_rifle")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("elec_ruined", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 47, coolingRate: 3, startingThermal: 71, shapeShare: 40);

            // Titan Rifle
            _builder.Create(RecipeType.TitanRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("tit_rifle")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 3)
                .ForgeParameters(rangeCenter: 53, rangeWidth: 41, coolingRate: 3, startingThermal: 68, shapeShare: 42);

            // Sith Rifle
            _builder.Create(RecipeType.SithRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("sith_rifle")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 45, coolingRate: 1, startingThermal: 69, shapeShare: 37);

            // Delta Rifle
            _builder.Create(RecipeType.DeltaRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("del_rifle")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 39, coolingRate: 3, startingThermal: 57, shapeShare: 38);

            // Proto Rifle
            _builder.Create(RecipeType.ProtoRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("proto_rifle")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("elec_imperfect", 3)
                .ForgeParameters(rangeCenter: 53, rangeWidth: 27, coolingRate: 5, startingThermal: 56, shapeShare: 39);

            // Ophidian Rifle
            _builder.Create(RecipeType.OphidianRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("oph_rifle")
                .Level(49)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 3)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 24, coolingRate: 5, startingThermal: 58, shapeShare: 39);

            // Chiro Rifle
            _builder.Create(RecipeType.ChiroRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("chi_rifle")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 27, coolingRate: 7, startingThermal: 58, shapeShare: 40);

            // Bol Rifle
            _builder.Create(RecipeType.BolRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("bol_rifle")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("bol_leather", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 22, coolingRate: 6, startingThermal: 54, shapeShare: 38);
        }
        private void IntermediateKnifes()
        {
            // Field Knife
            _builder.Create(RecipeType.FieldKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("fld_knife")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 2)
                .Component("lth_ruined", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 53, coolingRate: 3, startingThermal: 81, shapeShare: 39);

            // Veteran Knife
            _builder.Create(RecipeType.VeteranKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("vet_knife")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 2)
                .Component("lth_flawed", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 42, coolingRate: 3, startingThermal: 76, shapeShare: 40);

            // Prime Knife
            _builder.Create(RecipeType.PrimeKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("prm_knife")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 2)
                .Component("lth_good", 1)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 37, coolingRate: 4, startingThermal: 69, shapeShare: 44);

            // Ascendant Knife
            _builder.Create(RecipeType.AscendantKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("asc_knife")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 2)
                .Component("lth_imperfect", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 29, coolingRate: 5, startingThermal: 61, shapeShare: 39);

        }

        private void IntermediateLongswords()
        {
            // Field Longsword
            _builder.Create(RecipeType.FieldLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("fld_longsword")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3)
                .Component("lth_ruined", 2)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 52, coolingRate: 1, startingThermal: 81, shapeShare: 45);

            // Veteran Longsword
            _builder.Create(RecipeType.VeteranLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("vet_longsword")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3)
                .Component("lth_flawed", 2)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 41, coolingRate: 4, startingThermal: 69, shapeShare: 43);

            // Prime Longsword
            _builder.Create(RecipeType.PrimeLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("prm_longsword")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3)
                .Component("lth_good", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 41, coolingRate: 4, startingThermal: 66, shapeShare: 40);

            // Ascendant Longsword
            _builder.Create(RecipeType.AscendantLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("asc_longsword")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3)
                .Component("lth_imperfect", 2)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 30, coolingRate: 6, startingThermal: 64, shapeShare: 44);

        }

        private void IntermediateLightsabers()
        {
            // Field Electroblade
            _builder.Create(RecipeType.FieldElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("fld_electroblade")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 6)
                .Component("ref_veldite", 3)
                .Component("jade", 2)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 50, coolingRate: 3, startingThermal: 72, shapeShare: 40);

            // Veteran Electroblade
            _builder.Create(RecipeType.VeteranElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("vet_electroblade")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_flawed", 6)
                .Component("ref_scordspar", 3)
                .Component("agate", 2)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 47, coolingRate: 4, startingThermal: 62, shapeShare: 39);

            // Prime Electroblade
            _builder.Create(RecipeType.PrimeElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("prm_electroblade")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 6)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 38, coolingRate: 5, startingThermal: 55, shapeShare: 38);

            // Ascendant Electroblade
            _builder.Create(RecipeType.AscendantElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("asc_electroblade")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 6)
                .Component("ref_keromber", 3)
                .Component("ruby", 2)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 27, coolingRate: 5, startingThermal: 51, shapeShare: 38);

// Field Training Saber
            _builder.Create(RecipeType.FieldTrainingSaber, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("fld_trnsaber")
                .Level(13)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("jade", 6)
                .Component("ref_veldite", 3)
                .Component("elec_ruined", 5)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 52, coolingRate: 2, startingThermal: 68, shapeShare: 40);

            // Veteran Training Saber
            _builder.Create(RecipeType.VeteranTrainingSaber, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("vet_trnsaber")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("agate", 6)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 5)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 44, coolingRate: 2, startingThermal: 63, shapeShare: 35);

            // Prime Training Saber
            _builder.Create(RecipeType.PrimeTrainingSaber, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("prm_trnsaber")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("citrine", 6)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 5)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 34, coolingRate: 4, startingThermal: 60, shapeShare: 40);

            // Ascendant Training Saber
            _builder.Create(RecipeType.AscendantTrainingSaber, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("asc_trnsaber")
                .Level(43)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ruby", 6)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 5)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 26, coolingRate: 6, startingThermal: 51, shapeShare: 36);

        }

        private void IntermediateGreatSwords()
        {
            // Field Great Sword
            _builder.Create(RecipeType.FieldGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("fld_greatsword")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 8)
                .Component("wood", 5)
                .Component("lth_ruined", 3)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 51, coolingRate: 3, startingThermal: 72, shapeShare: 45);

            // Veteran Great Sword
            _builder.Create(RecipeType.VeteranGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("vet_greatsword")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 8)
                .Component("fine_wood", 5)
                .Component("lth_flawed", 3)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 38, coolingRate: 3, startingThermal: 73, shapeShare: 39);

            // Prime Great Sword
            _builder.Create(RecipeType.PrimeGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("prm_greatsword")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 8)
                .Component("ancient_wood", 5)
                .Component("lth_good", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 39, coolingRate: 3, startingThermal: 61, shapeShare: 43);

            // Ascendant Great Sword
            _builder.Create(RecipeType.AscendantGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("asc_greatsword")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 8)
                .Component("aracia_wood", 5)
                .Component("lth_imperfect", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 32, coolingRate: 6, startingThermal: 66, shapeShare: 42);

        }

        private void IntermediateSpears()
        {
            // Field Spear
            _builder.Create(RecipeType.FieldSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("fld_spear")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 6)
                .Component("wood", 3)
                .Component("lth_ruined", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 51, coolingRate: 2, startingThermal: 71, shapeShare: 43);

            // Veteran Spear
            _builder.Create(RecipeType.VeteranSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("vet_spear")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 6)
                .Component("fine_wood", 3)
                .Component("lth_flawed", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 39, coolingRate: 3, startingThermal: 71, shapeShare: 45);

            // Prime Spear
            _builder.Create(RecipeType.PrimeSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("prm_spear")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 6)
                .Component("ancient_wood", 3)
                .Component("lth_good", 2)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 33, coolingRate: 4, startingThermal: 62, shapeShare: 41);

            // Ascendant Spear
            _builder.Create(RecipeType.AscendantSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("asc_spear")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 3)
                .Component("lth_imperfect", 2)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 30, coolingRate: 4, startingThermal: 59, shapeShare: 44);

        }

        private void IntermediateTwinBlades()
        {
            // Field Twin Blade
            _builder.Create(RecipeType.FieldTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("fld_twinblade")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3)
                .Component("lth_ruined", 3)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 51, coolingRate: 3, startingThermal: 70, shapeShare: 42);

            // Veteran Twin Blade
            _builder.Create(RecipeType.VeteranTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("vet_twinblade")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3)
                .Component("lth_flawed", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 41, coolingRate: 3, startingThermal: 72, shapeShare: 44);

            // Prime Twin Blade
            _builder.Create(RecipeType.PrimeTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("prm_twinblade")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3)
                .Component("lth_good", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 34, coolingRate: 4, startingThermal: 64, shapeShare: 42);

            // Ascendant Twin Blade
            _builder.Create(RecipeType.AscendantTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("asc_twinblade")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3)
                .Component("lth_imperfect", 3)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 33, coolingRate: 5, startingThermal: 65, shapeShare: 39);

        }

        private void IntermediateSaberstaffs()
        {
            // Field Twin Electroblade
            _builder.Create(RecipeType.FieldTwinElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("fld_twinelec")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 8)
                .Component("ref_veldite", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 45, coolingRate: 1, startingThermal: 67, shapeShare: 36);

            // Veteran Twin Electroblade
            _builder.Create(RecipeType.VeteranTwinElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("vet_twinelec")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_flawed", 8)
                .Component("ref_scordspar", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 45, coolingRate: 4, startingThermal: 59, shapeShare: 35);

            // Prime Twin Electroblade
            _builder.Create(RecipeType.PrimeTwinElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("prm_twinelec")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 8)
                .Component("ref_plagionite", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 53, rangeWidth: 39, coolingRate: 5, startingThermal: 56, shapeShare: 39);

            // Ascendant Twin Electroblade
            _builder.Create(RecipeType.AscendantTwinElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("asc_twinelec")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 8)
                .Component("ref_keromber", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 53, rangeWidth: 27, coolingRate: 5, startingThermal: 52, shapeShare: 39);

// Field Training Saberstaff
            _builder.Create(RecipeType.FieldTrainingSaberstaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("fld_trnsabstaff")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("jade", 8)
                .Component("ref_veldite", 5)
                .Component("elec_ruined", 6)
                .Component("fiberp_ruined", 3)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 52, coolingRate: 2, startingThermal: 67, shapeShare: 35);

            // Veteran Training Saberstaff
            _builder.Create(RecipeType.VeteranTrainingSaberstaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("vet_trnsabstaff")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("agate", 8)
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 6)
                .Component("fiberp_flawed", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 41, coolingRate: 2, startingThermal: 58, shapeShare: 40);

            // Prime Training Saberstaff
            _builder.Create(RecipeType.PrimeTrainingSaberstaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("prm_trnsabstaff")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("citrine", 8)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 6)
                .Component("fiberp_good", 3)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 34, coolingRate: 4, startingThermal: 58, shapeShare: 36);

            // Ascendant Training Saberstaff
            _builder.Create(RecipeType.AscendantTrainingSaberstaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("asc_trnsabstaff")
                .Level(44)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ruby", 8)
                .Component("ref_keromber", 5)
                .Component("elec_imperfect", 6)
                .Component("fiberp_imperfect", 3)
                .ForgeParameters(rangeCenter: 53, rangeWidth: 28, coolingRate: 6, startingThermal: 55, shapeShare: 36);

        }

        private void IntermediateKatars()
        {
            // Field Katar
            _builder.Create(RecipeType.FieldKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("fld_katar")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2)
                .Component("lth_ruined", 1)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 48, coolingRate: 1, startingThermal: 77, shapeShare: 40);

            // Veteran Katar
            _builder.Create(RecipeType.VeteranKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("vet_katar")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .Component("lth_flawed", 1)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 42, coolingRate: 2, startingThermal: 74, shapeShare: 41);

            // Prime Katar
            _builder.Create(RecipeType.PrimeKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("prm_katar")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2)
                .Component("lth_good", 1)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 35, coolingRate: 5, startingThermal: 66, shapeShare: 43);

            // Ascendant Katar
            _builder.Create(RecipeType.AscendantKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("asc_katar")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2)
                .Component("lth_imperfect", 1)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 32, coolingRate: 4, startingThermal: 65, shapeShare: 41);

        }

        private void IntermediateStaffs()
        {
            // Field Staff
            _builder.Create(RecipeType.FieldStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("fld_staff")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3)
                .Component("lth_ruined", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 50, coolingRate: 1, startingThermal: 74, shapeShare: 39);

            // Veteran Staff
            _builder.Create(RecipeType.VeteranStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("vet_staff")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3)
                .Component("lth_flawed", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 47, coolingRate: 3, startingThermal: 70, shapeShare: 39);

            // Prime Staff
            _builder.Create(RecipeType.PrimeStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("prm_staff")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3)
                .Component("lth_good", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 41, coolingRate: 5, startingThermal: 64, shapeShare: 42);

            // Ascendant Staff
            _builder.Create(RecipeType.AscendantStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("asc_staff")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3)
                .Component("lth_imperfect", 2)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 28, coolingRate: 4, startingThermal: 59, shapeShare: 41);

        }

        private void IntermediatePistols()
        {
            // Field Pistol
            _builder.Create(RecipeType.FieldPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("fld_pistol")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 6)
                .Component("elec_ruined", 3)
                .Component("jade", 1)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 46, coolingRate: 2, startingThermal: 67, shapeShare: 38);

            // Veteran Pistol
            _builder.Create(RecipeType.VeteranPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("vet_pistol")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 6)
                .Component("elec_flawed", 3)
                .Component("agate", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 42, coolingRate: 4, startingThermal: 64, shapeShare: 42);

            // Prime Pistol
            _builder.Create(RecipeType.PrimePistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("prm_pistol")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 6)
                .Component("elec_good", 3)
                .Component("citrine", 1)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 34, coolingRate: 4, startingThermal: 63, shapeShare: 37);

            // Ascendant Pistol
            _builder.Create(RecipeType.AscendantPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("asc_pistol")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 6)
                .Component("elec_imperfect", 3)
                .Component("ruby", 1)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 27, coolingRate: 4, startingThermal: 55, shapeShare: 42);

        }

        private void IntermediateShurikens()
        {
            // Field Shuriken
            _builder.Create(RecipeType.FieldShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("fld_shuriken")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2)
                .Component("lth_ruined", 1)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 56, coolingRate: 2, startingThermal: 79, shapeShare: 40);

            // Veteran Shuriken
            _builder.Create(RecipeType.VeteranShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("vet_shuriken")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2)
                .Component("lth_flawed", 1)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 43, coolingRate: 4, startingThermal: 74, shapeShare: 44);

            // Prime Shuriken
            _builder.Create(RecipeType.PrimeShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("prm_shuriken")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2)
                .Component("lth_good", 1)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 42, coolingRate: 5, startingThermal: 69, shapeShare: 43);

            // Ascendant Shuriken
            _builder.Create(RecipeType.AscendantShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("asc_shuriken")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2)
                .Component("lth_imperfect", 1)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 37, coolingRate: 4, startingThermal: 63, shapeShare: 45);

        }

        private void IntermediateRifles()
        {
            // Field Rifle
            _builder.Create(RecipeType.FieldRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("fld_rifle")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 8)
                .Component("elec_ruined", 5)
                .Component("jade", 2)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 52, coolingRate: 2, startingThermal: 67, shapeShare: 40);

            // Veteran Rifle
            _builder.Create(RecipeType.VeteranRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("vet_rifle")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 8)
                .Component("elec_flawed", 5)
                .Component("agate", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 37, coolingRate: 4, startingThermal: 62, shapeShare: 38);

            // Prime Rifle
            _builder.Create(RecipeType.PrimeRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("prm_rifle")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 8)
                .Component("elec_good", 5)
                .Component("citrine", 2)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 35, coolingRate: 4, startingThermal: 63, shapeShare: 42);

            // Ascendant Rifle
            _builder.Create(RecipeType.AscendantRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("asc_rifle")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 8)
                .Component("elec_imperfect", 5)
                .Component("ruby", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 30, coolingRate: 5, startingThermal: 53, shapeShare: 39);

        }

    }
}
