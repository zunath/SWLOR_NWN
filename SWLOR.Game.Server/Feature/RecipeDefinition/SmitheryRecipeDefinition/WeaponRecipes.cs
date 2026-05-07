using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class WeaponRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Knifes();
            Longswords();
            Lightsabers();
            GreatSwords();
            Spears();
            TwinBlades();
            Saberstaffs();
            Katars();
            Staffs();
            Pistols();
            Shurikens();
            Rifles();

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
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 1)
                .Component("wood", 1);

            // Titan Knife
            _builder.Create(RecipeType.TitanKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("tit_knife")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 1)
                .Component("fine_wood", 1);

            // Sith Knife
            _builder.Create(RecipeType.SithKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("sith_knife")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Delta Knife
            _builder.Create(RecipeType.DeltaKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("del_knife")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 1)
                .Component("ancient_wood", 1);

            // Proto Knife
            _builder.Create(RecipeType.ProtoKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("proto_knife")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 1)
                .Component("aracia_wood", 1);

            // Ophidian Knife
            _builder.Create(RecipeType.OphidianKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("oph_knife")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 1)
                .Component("hyphae_wood", 1);

            // Chiro Knife
            _builder.Create(RecipeType.ChiroKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("chi_knife")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2);

            // Titan Longsword
            _builder.Create(RecipeType.TitanLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("tit_longsword")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Sith Longsword
            _builder.Create(RecipeType.SithLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("sith_longsword")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3);

            // Delta Longsword
            _builder.Create(RecipeType.DeltaLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("del_longsword")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Proto Longsword
            _builder.Create(RecipeType.ProtoLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("pro_longsword")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Ophidian Longsword
            _builder.Create(RecipeType.OphidianLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("oph_longsword")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Chiro Longsword
            _builder.Create(RecipeType.ChiroLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("chi_longsword")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 4)
                .Component("ref_veldite", 2);

            // Electroblade II
            _builder.Create(RecipeType.Electroblade2, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_flawed", 4)
                .Component("ref_scordspar", 2);

            // Sith Electroblade
            _builder.Create(RecipeType.SithElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("sith_electro")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_flawed", 5)
                .Component("ref_scordspar", 3);

            // Electroblade III
            _builder.Create(RecipeType.Electroblade3, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 4)
                .Component("ref_plagionite", 2);

            // Electroblade IV
            _builder.Create(RecipeType.Electroblade4, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 4)
                .Component("ref_keromber", 2);

            // Electroblade V
            _builder.Create(RecipeType.Electroblade5, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("electroblade_5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_high", 4)
                .Component("ref_jasioclase", 2);

            // Chiro Electroblade
            _builder.Create(RecipeType.ChiroElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("chi_electroblade")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);

            // Training Saber I
            _builder.Create(RecipeType.TrainingSaber1, SkillType.Smithery)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_train_1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
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
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
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
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
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
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
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
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.None, 0)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }

        private void GreatSwords()
        {
            // Basic Great Sword
            _builder.Create(RecipeType.BasicGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("b_greatsword")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3);

            // Titan Great Sword
            _builder.Create(RecipeType.TitanGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("tit_greatsword")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3);

            // Sith Great Sword
            _builder.Create(RecipeType.SithGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("sith_gswd")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Great Sword
            _builder.Create(RecipeType.DeltaGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("del_greatsword")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);

            // Proto Great Sword
            _builder.Create(RecipeType.ProtoGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("proto_greatsword")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Ophidian Great Sword
            _builder.Create(RecipeType.OphidianGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("oph_greatsword")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 3);

            // Chiro Great Sword
            _builder.Create(RecipeType.ChiroGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("chi_greatsword")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);
        }

        private void Spears()
        {
            // Basic Spear
            _builder.Create(RecipeType.BasicSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("b_spear")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 2);

            // Titan Spear
            _builder.Create(RecipeType.TitanSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("tit_spear")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Sith Spear
            _builder.Create(RecipeType.SithSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("sith_spear")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Spear
            _builder.Create(RecipeType.DeltaSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("del_spear")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);

            // Proto Spear
            _builder.Create(RecipeType.ProtoSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("proto_spear")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2);

            // Ophidian Spear
            _builder.Create(RecipeType.OphidianSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("oph_spear")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("aracia_wood", 4)
                .Component("hyphae_wood", 2);

            // Chiro Spear
            _builder.Create(RecipeType.ChiroSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("chi_spear")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);

            // Alchemized Spear
            _builder.Create(RecipeType.AlchemizedSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("alc_spear")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("frogguts", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("stolen_s_artifact", 5)
                .Component("emerald", 5)
                .Component("tukata_hide", 5)
                .Component("froglegs", 2);
        }

        private void TwinBlades()
        {
            // Basic Twin Blade
            _builder.Create(RecipeType.BasicTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("b_twinblade")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2);

            // Titan Twin Blade
            _builder.Create(RecipeType.TitanTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("tit_twinblade")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Sith Twin Blade
            _builder.Create(RecipeType.SithTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("sith_twinblade")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Delta Twin Blade
            _builder.Create(RecipeType.DeltaTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("del_twinblade")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Proto Twin Blade
            _builder.Create(RecipeType.ProtoTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("proto_twinblade")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Ophidian Twin Blade
            _builder.Create(RecipeType.OphidianTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("oph_twinblade")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Chiro Twin Blade
            _builder.Create(RecipeType.ChiroTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("chi_twinblade")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);
        }

        private void Saberstaffs()
        {
            // Twin Electroblade I
            _builder.Create(RecipeType.TwinElectroblade1, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 5)
                .Component("ref_veldite", 3);

            // Twin Electroblade II
            _builder.Create(RecipeType.TwinElectroblade2, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_flawed", 5)
                .Component("ref_scordspar", 3);

            // Twin Electroblade III
            _builder.Create(RecipeType.TwinElectroblade3, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 5)
                .Component("ref_plagionite", 3);

            // Twin Electroblade IV
            _builder.Create(RecipeType.TwinElectroblade4, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 5)
                .Component("ref_keromber", 3);

            // Twin Electroblade V
            _builder.Create(RecipeType.TwinElectroblade5, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_high", 5)
                .Component("ref_jasioclase", 3);

            // Chiro Twin Electroblade
            _builder.Create(RecipeType.ChiroTwinElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("chi_twinelec")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);

            // Training Saberstaff I
            _builder.Create(RecipeType.TrainingSaberstaff1, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_1")
                .Level(9)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("jade", 5)
                .Component("ref_veldite", 3)
                .Component("elec_ruined", 4);

            // Training Saberstaff II
            _builder.Create(RecipeType.TrainingSaberstaff2, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_2")
                .Level(19)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("agate", 5)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 4);

            // Training Saberstaff III
            _builder.Create(RecipeType.TrainingSaberstaff3, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_3")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("citrine", 5)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 4);

            // Training Saberstaff IV
            _builder.Create(RecipeType.TrainingSaberstaff4, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_4")
                .Level(39)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ruby", 5)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 4);

            // Training Saberstaff V
            _builder.Create(RecipeType.TrainingSaberstaff5, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("trn_saberstaff_5")
                .Level(49)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("emerald", 5)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 4);

            // Saberstaff Upgrade I
            _builder.Create(RecipeType.SaberstaffUpgradeKit1, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("saberstaff_upg1")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.None, 0)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }

        private void Katars()
        {
            // Basic Katar
            _builder.Create(RecipeType.BasicKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("b_katar")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 1);

            // Titan Katar
            _builder.Create(RecipeType.TitanKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("tit_katar")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);

            // Sith Katar
            _builder.Create(RecipeType.SithKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("sith_katar")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Katar
            _builder.Create(RecipeType.DeltaKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("del_katar")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);

            // Proto Katar
            _builder.Create(RecipeType.ProtoKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("proto_katar")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1);

            // Ophidian Katar
            _builder.Create(RecipeType.OphidianKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("oph_katar")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);

            // Chiro Katar
            _builder.Create(RecipeType.ChiroKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("chi_katar")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);
        }

        private void Staffs()
        {
            // Basic Staff
            _builder.Create(RecipeType.BasicStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("b_staff")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2);

            // Titan Staff
            _builder.Create(RecipeType.TitanStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("tit_staff")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Sith Staff
            _builder.Create(RecipeType.SithStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("sith_staff")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);

            // Delta Staff
            _builder.Create(RecipeType.DeltaStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("del_staff")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Proto Staff
            _builder.Create(RecipeType.ProtoStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("proto_staff")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Ophidian Staff
            _builder.Create(RecipeType.OphidianStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("oph_staff")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Chiro Staff
            _builder.Create(RecipeType.ChiroStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("chi_staff")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);

        }

        private void Pistols()
        {
            // Basic Pistol
            _builder.Create(RecipeType.BasicPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("b_pistol")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 4)
                .Component("elec_ruined", 2);

            // Titan Pistol
            _builder.Create(RecipeType.TitanPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("tit_pistol")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 4)
                .Component("elec_flawed", 2);

            // Sith Pistol
            _builder.Create(RecipeType.SithPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("sith_pistol")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 3);

            // Delta Pistol
            _builder.Create(RecipeType.DeltaPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("del_pistol")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 4)
                .Component("elec_good", 2);

            // Proto Pistol
            _builder.Create(RecipeType.ProtoPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("proto_pistol")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 2);

            // Ophidian Pistol
            _builder.Create(RecipeType.OphidianPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("oph_pistol")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 4)
                .Component("elec_high", 2);

            // Chiro Pistol
            _builder.Create(RecipeType.ChiroPistol, SkillType.Smithery)
                .Category(RecipeCategoryType.Pistol)
                .Resref("chi_pistol")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);
        }

        private void Shurikens()
        {
            // Basic Shuriken
            _builder.Create(RecipeType.BasicShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("b_shuriken")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 1);

            // Titan Shuriken
            _builder.Create(RecipeType.TitanShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("tit_shuriken")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);

            // Sith Shuriken
            _builder.Create(RecipeType.SithShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("sith_shuriken")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Shuriken
            _builder.Create(RecipeType.DeltaShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("del_shuriken")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);

            // Proto Shuriken
            _builder.Create(RecipeType.ProtoShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("proto_shuriken")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1);

            // Ophidian Shuriken
            _builder.Create(RecipeType.OphidianShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("oph_shuriken")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);

            // Chiro Shuriken
            _builder.Create(RecipeType.ChiroShuriken, SkillType.Smithery)
                .Category(RecipeCategoryType.Shuriken)
                .Resref("chi_shuriken")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);
        }

        private void Rifles()
        {
            // Basic Rifle
            _builder.Create(RecipeType.BasicRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("b_rifle")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("elec_ruined", 3);

            // Titan Rifle
            _builder.Create(RecipeType.TitanRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("tit_rifle")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("elec_flawed", 3);

            // Sith Rifle
            _builder.Create(RecipeType.SithRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("sith_rifle")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 2);

            // Delta Rifle
            _builder.Create(RecipeType.DeltaRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("del_rifle")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3);

            // Proto Rifle
            _builder.Create(RecipeType.ProtoRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("proto_rifle")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("elec_imperfect", 3);

            // Ophidian Rifle
            _builder.Create(RecipeType.OphidianRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("oph_rifle")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 3);

            // Chiro Rifle
            _builder.Create(RecipeType.ChiroRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("chi_rifle")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);

            // Bol Rifle
            _builder.Create(RecipeType.BolRifle, SkillType.Smithery)
                .Category(RecipeCategoryType.Rifle)
                .Resref("bol_rifle")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.WeaponBlueprints, 5)
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
                .Component("ref_keromber", 5);
        }
    }
}