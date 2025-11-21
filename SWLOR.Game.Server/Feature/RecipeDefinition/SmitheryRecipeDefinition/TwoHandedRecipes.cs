using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class TwoHandedRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            GreatSwords();
            Spears();
            TwinBlades();
            Saberstaffs();

            return _builder.Build();
        }

        private void GreatSwords()
        {
            // Basic Great Sword
            _builder.Create(RecipeType.BasicGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("b_greatsword")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3);

            // Titan Great Sword
            _builder.Create(RecipeType.TitanGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("tit_greatsword")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3);

            // Sith Great Sword
            _builder.Create(RecipeType.SithGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("sith_gswd")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Great Sword
            _builder.Create(RecipeType.DeltaGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("del_greatsword")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);

            // Proto Great Sword
            _builder.Create(RecipeType.ProtoGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("proto_greatsword")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Ophidian Great Sword
            _builder.Create(RecipeType.OphidianGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("oph_greatsword")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 3);

            // Chiro Great Sword
            _builder.Create(RecipeType.ChiroGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("chi_greatsword")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 2);

            // Titan Spear
            _builder.Create(RecipeType.TitanSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("tit_spear")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Sith Spear
            _builder.Create(RecipeType.SithSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("sith_spear")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Spear
            _builder.Create(RecipeType.DeltaSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("del_spear")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);

            // Proto Spear
            _builder.Create(RecipeType.ProtoSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("proto_spear")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2);

            // Ophidian Spear
            _builder.Create(RecipeType.OphidianSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("oph_spear")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("aracia_wood", 4)
                .Component("hyphae_wood", 2);

            // Chiro Spear
            _builder.Create(RecipeType.ChiroSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("chi_spear")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2);

            // Titan Twin Blade
            _builder.Create(RecipeType.TitanTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("tit_twinblade")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Sith Twin Blade
            _builder.Create(RecipeType.SithTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("sith_twinblade")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Delta Twin Blade
            _builder.Create(RecipeType.DeltaTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("del_twinblade")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Proto Twin Blade
            _builder.Create(RecipeType.ProtoTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("proto_twinblade")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Ophidian Twin Blade
            _builder.Create(RecipeType.OphidianTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("oph_twinblade")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Chiro Twin Blade
            _builder.Create(RecipeType.ChiroTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("chi_twinblade")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_ruined", 5)
                .Component("ref_veldite", 3);

            // Twin Electroblade II
            _builder.Create(RecipeType.TwinElectroblade2, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("elec_flawed", 5)
                .Component("ref_scordspar", 3);

            // Twin Electroblade III
            _builder.Create(RecipeType.TwinElectroblade3, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_good", 5)
                .Component("ref_plagionite", 3);

            // Twin Electroblade IV
            _builder.Create(RecipeType.TwinElectroblade4, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_imperfect", 5)
                .Component("ref_keromber", 3);

            // Twin Electroblade V
            _builder.Create(RecipeType.TwinElectroblade5, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("twin_elec_5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("elec_high", 5)
                .Component("ref_jasioclase", 3);

            // Chiro Twin Electroblade
            _builder.Create(RecipeType.ChiroTwinElectroblade, SkillType.Smithery)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("chi_twinelec")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
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
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
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


    }
}