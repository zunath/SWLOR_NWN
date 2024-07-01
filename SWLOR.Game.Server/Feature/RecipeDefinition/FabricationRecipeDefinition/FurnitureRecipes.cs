using System;
using System.Collections.Generic;
using System.Reactive;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class FurnitureRecipes : IRecipeListDefinition
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
            // Bed Roll
            _builder.Create(RecipeType.BedRoll, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0085")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 1)
                .Component("fiberp_ruined", 1);

            // Easel
            _builder.Create(RecipeType.Easel, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0045")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 1)
                .Component("lth_ruined", 1);

            // Bench
            _builder.Create(RecipeType.Bench, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0132")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 1)
                .Component("ref_veldite", 1);

            // Candle
            _builder.Create(RecipeType.Candle, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0062")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1);

            // Campfire
            _builder.Create(RecipeType.Campfire, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0011")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 1)
                .Component("wood", 1);

            // Carpet
            _builder.Create(RecipeType.Carpet, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0077")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1);

            // Banner, Wall, Lizard
            _builder.Create(RecipeType.BannerWallLizard, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0133")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 2)
                .Component("ref_veldite", 1);

            // Weapon Rack
            _builder.Create(RecipeType.WeaponRack, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0215")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 2)
                .Component("ref_veldite", 1);

            // Table, Plastic (Large)
            _builder.Create(RecipeType.TablePlasticLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0233")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("wood", 2);

            // Cot
            _builder.Create(RecipeType.Cot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0069")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1);

            // Keg
            _builder.Create(RecipeType.Keg, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0047")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 2)
                .Component("ref_veldite", 1);

            // Chair, Wood, Small
            _builder.Create(RecipeType.ChairWoodSmall, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0120")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 2)
                .Component("ref_veldite", 1);

            // Rope Coil
            _builder.Create(RecipeType.RopeCoil, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0023")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 1);

            // Throw Rug
            _builder.Create(RecipeType.ThrowRug, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0072")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 1);

            // Chair, Wood
            _builder.Create(RecipeType.ChairWood, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0119")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 1);

            // Stuffed Toy, Bantha
            _builder.Create(RecipeType.StuffedToyBantha, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0201")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 1);

            // Cushions
            _builder.Create(RecipeType.Cushions, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0061")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2);

            // Table, Wood
            _builder.Create(RecipeType.TableWood, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0070")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2);

            // Bench, Wood, Small
            _builder.Create(RecipeType.BenchWoodSmall, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0118")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2);

            // Table, Darkwood
            _builder.Create(RecipeType.TableDarkwood, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0015")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2);

            // Table, Wood, With Fish
            _builder.Create(RecipeType.TableWoodWithFish, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0057")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("ref_veldite", 2);

            // Hand Chair
            _builder.Create(RecipeType.HandChair, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0059")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 2);

            // Footstool
            _builder.Create(RecipeType.Footstool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0107")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("ref_veldite", 2);

            // Ornament, Solar System
            _builder.Create(RecipeType.OrnamentSolarSystem, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0176")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 4)
                .Component("ref_veldite", 2);

            // Bench, Elegant, Grey
            _builder.Create(RecipeType.BenchElegantGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0258")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("ref_veldite", 2);

            // Pedestal
            _builder.Create(RecipeType.Pedestal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0022")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("ref_veldite", 2);

            // Tome
            _builder.Create(RecipeType.Tome, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0080")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2);

            // Potted Plant
            _builder.Create(RecipeType.PottedPlant, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0091")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("ref_veldite", 2);

            // Fridge, Worn
            _builder.Create(RecipeType.FridgeWorn, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0216")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 4)
                .Component("elec_ruined", 2);

            // Table, Coffee, Shelf
            _builder.Create(RecipeType.TableCoffeeShelf, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0016")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 3);

            // Net
            _builder.Create(RecipeType.Net, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0051")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 2);

            // Gong
            _builder.Create(RecipeType.Gong, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0013")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 2);

            // Chair, Open Frame (Brown)
            _builder.Create(RecipeType.ChairOpenFrameBrown, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0144")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("ref_veldite", 2);

            // Toilet, White /w Cistern
            _builder.Create(RecipeType.ToiletWhiteWithCistern, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0210")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 2);

            // Table, Round, Oak
            _builder.Create(RecipeType.TableRoundOak, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0234")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 2)
                .Component("wood", 4);

            // Umbrella, Blue
            _builder.Create(RecipeType.UmbrellaBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0049")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("lth_ruined", 4);

            // Umbrella, Red
            _builder.Create(RecipeType.UmbrellaRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0050")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("lth_ruined", 4);

            // Trash Can
            _builder.Create(RecipeType.TrashCan, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0288")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 3);

            // Doorway, Metal
            _builder.Create(RecipeType.DoorwayMetal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Doors)
                .Resref("structure_0019")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3);

            // Bird Cage
            _builder.Create(RecipeType.BirdCage, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0081")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3);

            // Chair, Pedestal /w Arms
            _builder.Create(RecipeType.ChairPedestalWithArms, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0145")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("ref_veldite", 3);

            // Cot /w Table
            _builder.Create(RecipeType.CotWithTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0150")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 5)
                .Component("wood", 3);

            // Pile of Cushions (White)
            _builder.Create(RecipeType.PileOfCushionsWhite, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0225")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 5);

            // Lamp, Bars
            _builder.Create(RecipeType.LampBars, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0034")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 5);

            // Screen, Closed
            _builder.Create(RecipeType.ScreenClosed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0289")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("lth_ruined", 5);

            // Screen, Open
            _builder.Create(RecipeType.ScreenOpen, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0290")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("lth_ruined", 5);

            // Torch Bracket
            _builder.Create(RecipeType.TorchBracket, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0039")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 6)
                .Component("wood", 3);

            // Obelisk, Small
            _builder.Create(RecipeType.ObeliskSmall, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0006")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 6)
                .Component("wood", 3);

            // Table, Wood, Large
            _builder.Create(RecipeType.TableWoodLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0056")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 6)
                .Component("ref_veldite", 3);

            // Space Suit (Tan)
            _builder.Create(RecipeType.SpaceSuitTan, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0192")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 6)
                .Component("wood", 3);

            // Chair, Desk (Light Gray)
            _builder.Create(RecipeType.ChairDeskBlackDarkGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0291")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("fiberp_ruined", 5);

            // Lamp, Bars (Dark)
            _builder.Create(RecipeType.LampBarsDark, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0292")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 5);

            // DNA Extractor I
            _builder.Create(RecipeType.DNAExtractor1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Tool)
                .Resref("dna_extractor_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_tilarium", 3)
                .Component("elec_ruined", 2);
        }

        private void Tier2()
        {
            // Ladder, Light
            _builder.Create(RecipeType.LadderLight, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0007")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 1)
                .Component("ref_scordspar", 1);

            // Birdbath
            _builder.Create(RecipeType.Birdbath, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0026")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 1)
                .Component("lth_flawed", 1);

            // Bench, Wood
            _builder.Create(RecipeType.BenchWood, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0113")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 1)
                .Component("ref_scordspar", 1);

            // Shower, White
            _builder.Create(RecipeType.ShowerWhite, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0239")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 2)
                .Component("ref_scordspar", 1);

            // Shower, Floor Basin
            _builder.Create(RecipeType.ShowerFloorBasin, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0240")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("ref_scordspar", 2);

            // Ladder, Dark
            _builder.Create(RecipeType.LadderDark, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0008")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("ref_scordspar", 1);

            // Pillar, Wood, Dark
            _builder.Create(RecipeType.PillarWoodDark, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0082")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("ref_scordspar", 1);

            // Statue, Twi'lek
            _builder.Create(RecipeType.StatueTwilek, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0127")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 2)
                .Component("lth_flawed", 1);

            // Wooden Wall, Planks (Small)
            _builder.Create(RecipeType.MetalWallSinglePipes, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0244")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("fiberp_flawed", 1);

            // Bed, Low (Blue)
            _builder.Create(RecipeType.BedLowBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0109")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("lth_flawed", 2);

            // Bed, Low (Red)
            _builder.Create(RecipeType.BedLowRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0110")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("lth_flawed", 2);

            // Pile Of Cushions, Square
            _builder.Create(RecipeType.PileOfCushionsSquare, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0287")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 1)
                .Component("lth_flawed", 2);

            // Window
            _builder.Create(RecipeType.Window, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0060")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("ref_scordspar", 1);

            // Pedestal, Evil
            _builder.Create(RecipeType.PedestalEvil, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0025")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 2)
                .Component("lth_flawed", 1);

            // Cabinet, Curved (Grey/White)
            _builder.Create(RecipeType.CabinetCurvedGreyWhite, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0139")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 2)
                .Component("lth_flawed", 1);

            // Skeleton, Medical Display
            _builder.Create(RecipeType.SkeletonMedicalDisplay, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0190")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 2)
                .Component("lth_flawed", 1);

            // Oven
            _builder.Create(RecipeType.Oven, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0226")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 2)
                .Component("elec_flawed", 4);

            // Bunk bed (Metal) Grey
            _builder.Create(RecipeType.BunkBedMetalGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0238")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 2)
                .Component("lth_flawed", 4);

            // Chair Plinth
            _builder.Create(RecipeType.ChairPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0261")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("ref_scordspar", 3);

            // Brazier, Round
            _builder.Create(RecipeType.BrazierRound, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0021")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 3)
                .Component("lth_flawed", 2);

            // Vase, Tall
            _builder.Create(RecipeType.VaseTall, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0084")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Bed, High Back (Black/Grey)
            _builder.Create(RecipeType.BedHighBackBlackGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0135")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 3)
                .Component("ref_scordspar", 2);

            // Dining Chair - Grey
            _builder.Create(RecipeType.ChairDiningGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0262")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("ref_scordspar", 3);

            // Dining Chair, Orange
            _builder.Create(RecipeType.ChairDiningOrange, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0264")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("ref_scordspar", 3);

            // Mat, Small Tatami
            _builder.Create(RecipeType.MatSmallTatami, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0046")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 2)
                .Component("fiberp_flawed", 3);

            // Brazier, Stone
            _builder.Create(RecipeType.BrazierStone, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0017")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Ottoman
            _builder.Create(RecipeType.Ottoman, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0086")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 3)
                .Component("lth_flawed", 2);

            // Bed, Side Table
            _builder.Create(RecipeType.BedSideTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0140")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 3)
                .Component("lth_flawed", 2);

            // Rug, Classic (Light Brown)
            _builder.Create(RecipeType.RugClassicLightBrown, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0224")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 2)
                .Component("lth_flawed", 3);

            // Metal Wall, Wide, Pipes
            _builder.Create(RecipeType.MetalWallWidePipes, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0255")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 3)
                .Component("fiberp_flawed", 2);

            // Chair, Wooden Padded
            _builder.Create(RecipeType.ChairWoodenPadded, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0053")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 3)
                .Component("ref_scordspar", 3);

            // Mat, Medium Tatami
            _builder.Create(RecipeType.MatMediumTatami, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0293")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 3)
                .Component("fiberp_flawed", 3);

            // Statue, Guardian
            _builder.Create(RecipeType.StatueGuardian, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0018")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Lamp Post
            _builder.Create(RecipeType.LampPost, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0038")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("fiberp_flawed", 2);

            // Bed, Low
            _builder.Create(RecipeType.BedLow, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0141")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("fiberp_flawed", 2);

            // Fridge, Dark
            _builder.Create(RecipeType.FridgeDark, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0217")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 4)
                .Component("elec_flawed", 2);

            // Couch, Leather Panels (Grey)
            _builder.Create(RecipeType.CouchLeatherPanelsGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0241")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("lth_flawed", 6);

            // Flaming Statue
            _builder.Create(RecipeType.FlamingStatue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0020")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Pillar, Rounded
            _builder.Create(RecipeType.PillarRounded, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0088")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("fiberp_flawed", 2);

            // Bed, High Back (Blue)
            _builder.Create(RecipeType.BedHighBackBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0136")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("ref_scordspar", 2);

            // Bed, Medical/Exam
            _builder.Create(RecipeType.BedMedicalExam, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0137")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("ref_scordspar", 2);

            // Mirror (Small)
            _builder.Create(RecipeType.MirrorSmall, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0174")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("ref_scordspar", 2);

            // Table, Wall, Oval
            _builder.Create(RecipeType.TableWallOval, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0209")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("ref_scordspar", 2);

            // Metal Wall, Door, Pipes
            _builder.Create(RecipeType.MetalWallDoorPipes, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0256")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 4)
                .Component("fiberp_flawed", 3);

            // Jukebox
            _builder.Create(RecipeType.Jukebox, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0005")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_flawed", 5)
                .Component("ref_scordspar", 3);

            // Painting 1
            _builder.Create(RecipeType.Painting1, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0092")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 5)
                .Component("fiberp_flawed", 3);

            // Banner, Standing
            _builder.Create(RecipeType.BannerStanding, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0142")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 5)
                .Component("fiberp_flawed", 3);

            // Ottoman, Decorated (Black)
            _builder.Create(RecipeType.OttomanDecoratedBlack, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0177")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 5)
                .Component("fiberp_flawed", 3);

            // Microwave (Black)
            _builder.Create(RecipeType.MicrowaveBlack, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0228")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 4)
                .Component("elec_flawed", 2);

            // Chair, Curved Form
            _builder.Create(RecipeType.ChairCurvedForm, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0063")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("ref_scordspar", 4);

            // Lantern, Floor Small
            _builder.Create(RecipeType.LanternFloorSmall, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0294")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("fiberp_flawed", 4);

            // Doorway, Stone
            _builder.Create(RecipeType.DoorwayStone, SkillType.Fabrication)
                .Category(RecipeCategoryType.Doors)
                .Resref("structure_0030")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3);

            // Pillar, Wood
            _builder.Create(RecipeType.PillarWood, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0040")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 5)
                .Component("fiberp_flawed", 3);

            // Bookshelf, Pedestal (White)
            _builder.Create(RecipeType.BookshelfPedestalWhite, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0138")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 5)
                .Component("ref_scordspar", 3);

            // Pot, Urn, Grecian
            _builder.Create(RecipeType.PotUrnGrecian, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0189")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 5)
                .Component("fiberp_flawed", 3);

            // Chair, Wooden Striped
            _builder.Create(RecipeType.ChairWoodenStriped, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0066")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 3)
                .Component("ref_scordspar", 5);

            // Table, Coffee, Wood /w Glass
            _builder.Create(RecipeType.TableCoffeeWoodwGlass, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0295")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 4);

            // Bed, Art Deco
            _builder.Create(RecipeType.BedArtDeco, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0296")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 4)
                .Component("lth_flawed", 4);

            // Statue, Monster
            _builder.Create(RecipeType.StatueMonster, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0014")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 6)
                .Component("lth_flawed", 3);

            // Candelabra
            _builder.Create(RecipeType.Candelabra, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0090")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 6)
                .Component("fiberp_flawed", 3);

            // Chair, Crew (Grey)
            _builder.Create(RecipeType.ChairCrewGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0143")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fine_wood", 6)
                .Component("fiberp_flawed", 3);

            // Monitor, Wall, Logo Display (Blue)
            _builder.Create(RecipeType.MonitorWallLogoDisplayBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0175")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_flawed", 6)
                .Component("ref_scordspar", 3);

            // Bathtub
            _builder.Create(RecipeType.Bathtub, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0134")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_good", 4)
                .Component("ref_plagionite", 2);

            // Wardrobe, Grey, Low
            _builder.Create(RecipeType.WardrobeGreyLow, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0297")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 4);

            // DNA Extractor II
            _builder.Create(RecipeType.DNAExtractor2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Tool)
                .Resref("dna_extractor_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_currian", 3)
                .Component("elec_flawed", 2);
        }

        private void Tier3()
        {

            // Obelisk, Large
            _builder.Create(RecipeType.ObeliskLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0004")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 1)
                .Component("ancient_wood", 1);

            // Gnomish Contraption
            _builder.Create(RecipeType.GnomishContraption, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0032")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 1)
                .Component("ancient_wood", 1);

            // Chair, Pedestal, Padded (Red)
            _builder.Create(RecipeType.ChairPedestalPaddedRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0146")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 1)
                .Component("ref_plagionite", 1);

            // Pot, Bush, Clipped
            _builder.Create(RecipeType.PotBushClipped, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0180")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 1)
                .Component("lth_good", 1);

            // Coffee Maker
            _builder.Create(RecipeType.CoffeeMaker, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0227")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 2)
                .Component("elec_flawed", 3);

            // Metal Wall, Single, Ribbed
            _builder.Create(RecipeType.MetalWallSingleRibbed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0252")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 2)
                .Component("fiberp_good", 1);

            // Female Statue
            _builder.Create(RecipeType.FemaleStatue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0031")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 1)
                .Component("lth_good", 1);

            // Vase, Rounded
            _builder.Create(RecipeType.VaseRounded, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0076")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 2)
                .Component("lth_good", 1);

            // Chair, Pedestal, Panel
            _builder.Create(RecipeType.ChairPedestalPanel, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0147")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 2)
                .Component("ref_plagionite", 1);

            // Pot, Bush, Flowers
            _builder.Create(RecipeType.PotBushFlowers, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0181")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 2)
                .Component("lth_good", 1);

            // Footlocker, Black
            _builder.Create(RecipeType.FootlockerBlack, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0071")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 1);

            // Pedestal, Sword
            _builder.Create(RecipeType.PedestalSword, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0029")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);

            // Bed, Wood, Yellow
            _builder.Create(RecipeType.BedWoodYellow, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0078")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 2)
                .Component("fiberp_good", 1);

            // Console, Floor Mounted (Blue Screens)
            _builder.Create(RecipeType.ConsoleFloorMountedBlueScreens, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0148")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_good", 2)
                .Component("ref_plagionite", 1);

            // Pot, Bush, Tall
            _builder.Create(RecipeType.PotBushTall, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0182")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 2)
                .Component("lth_good", 1);

            // Store Counter (Stained)
            _builder.Create(RecipeType.StoreCounterStained, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0236")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("fiberp_good", 2);

            // Metal Wall, Wide, Ribbed
            _builder.Create(RecipeType.MetalWallWideRibbed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0253")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("fiberp_good", 2);

            // Table, Steel, Stained
            _builder.Create(RecipeType.TableSteelStained, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0087")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 3);

            // Urn
            _builder.Create(RecipeType.Urn, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0067")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);

            // Pillar, Stone
            _builder.Create(RecipeType.PillarStone, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0033")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Console, Floor Mounted (Green Screens)
            _builder.Create(RecipeType.ConsoleFloorMountedGreenScreens, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0149")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_good", 3)
                .Component("ref_plagionite", 2);

            // Pipes, Conduit (with Power Controls)
            _builder.Create(RecipeType.PipesConduitWithPowerControls, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0178")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_good", 3)
                .Component("ref_plagionite", 2);

            // Metal Wall, Door, Ribbed
            _builder.Create(RecipeType.MetalWallDoorRibbed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0254")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("fiberp_good", 2);

            // Couch, Cushion, Grey/Red
            _builder.Create(RecipeType.CouchCushionGreyRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0257")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_flawed", 2)
                .Component("lth_flawed", 3);

            // Monitor, Overhead
            _builder.Create(RecipeType.MonitorOverhead, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0094")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 2)
                .Component("elec_good", 4);

            // Cage
            _builder.Create(RecipeType.Cage, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0037")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Overgrown Pillar
            _builder.Create(RecipeType.OvergrownPillar, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0079")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Floor-anchored shackles
            _builder.Create(RecipeType.FloorAnchoredShackles, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0100")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("lth_good", 2);

            // Pot, Clay Urn
            _builder.Create(RecipeType.PotClayUrn, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0183")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 3)
                .Component("lth_good", 2);

            // Cookpot
            _builder.Create(RecipeType.Cookpot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0219")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);

            // Bar White
            _builder.Create(RecipeType.BarWhite, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0229")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 5)
                .Component("fiberp_good", 3);

            // Table, Polygon Design
            _builder.Create(RecipeType.TablePolygonDesign, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0265")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_flawed", 2)
                .Component("ref_plagionite", 3);

            // Table, Round, Glass
            _builder.Create(RecipeType.TableRoundGlass, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0266")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_flawed", 4)
                .Component("ref_plagionite", 2);

            // Pillar, Power Control
            _builder.Create(RecipeType.PillarPowerControl, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0231")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 3);

            // Statue, Wyvern
            _builder.Create(RecipeType.StatueWyvern, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0024")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("lth_good", 2);

            // Bunk Bed
            _builder.Create(RecipeType.BunkBed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0083")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 4)
                .Component("fiberp_good", 2);

            // Chair, Chancellor
            _builder.Create(RecipeType.ChairChancellor, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0128")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 4)
                .Component("fiberp_good", 2);

            // Pot, Flower, Daisy
            _builder.Create(RecipeType.PotFlowerDaisy, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0184")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 4)
                .Component("lth_good", 2);

            // Engineering Terminal
            _builder.Create(RecipeType.EngineeringTerminal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0220")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3);

            // Fountain, Dark Grey
            _builder.Create(RecipeType.FountainDarkGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0298")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("fiberp_good", 3);

            // Canopy, Leather
            _builder.Create(RecipeType.CanopyLeather, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0299")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 4)
                .Component("lth_good", 3);

            // Fountain
            _builder.Create(RecipeType.Fountain, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0043")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("fiberp_good", 2);

            // Round Wooden Table
            _builder.Create(RecipeType.RoundWoodenTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0101")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 4)
                .Component("fiberp_good", 2);

            // Desk, Control Board Inlay
            _builder.Create(RecipeType.DeskControlBoardInlay, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0151")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_good", 4)
                .Component("ref_plagionite", 2);

            // Cylinder, Cross Top
            _builder.Create(RecipeType.CylinderCrossTop, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0179")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);

            // Fabrication Terminal
            _builder.Create(RecipeType.FabricationTerminal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0221")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3);

            // Chair, Dining, Gothic
            _builder.Create(RecipeType.ChairDiningGothic, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0105")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 3);

            // Console, Floor Mounted, Dark
            _builder.Create(RecipeType.ConsoleFloorMountedDark, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0300")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 4)
                .Component("elec_good", 4);

            // Altar, Stone
            _builder.Create(RecipeType.AltarStone, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0068")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 3);

            // Statue of Lathander
            _builder.Create(RecipeType.StatueOfLathander, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0035")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 3);

            // Statue, Robed Woman
            _builder.Create(RecipeType.StatueRobedWoman, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0129")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 3);

            // Pot, Flower, Yellow
            _builder.Create(RecipeType.PotFlowerYellow, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0185")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 5)
                .Component("lth_good", 3);

            // Refinery
            _builder.Create(RecipeType.Refinery, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0222")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3);

            // Television, Old Model
            _builder.Create(RecipeType.TelevisionOldModel, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0268")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_flawed", 2)
                .Component("elec_flawed", 3);

            // Carpet, Medallion
            _builder.Create(RecipeType.CarpetMedallion, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0232")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .Component("lth_good", 4)
                .Component("fiberp_good", 4);

            // Bear Skin Rug
            _builder.Create(RecipeType.BearSkinRug, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0064")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_good", 5)
                .Component("lth_good", 3);

            // Carpet, Fancy
            _builder.Create(RecipeType.CarpetFancy, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0093")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Desk, Control Center, Large Screen
            _builder.Create(RecipeType.DeskControlCenterLargeScreen, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0153")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_good", 5)
                .Component("ref_plagionite", 3);

            // Pot, Long Leaf 1
            _builder.Create(RecipeType.PotLongLeaf1, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0186")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 5)
                .Component("lth_good", 3);

            // Smithery Bench
            _builder.Create(RecipeType.SmitheryBench, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0223")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .Component("ref_plagionite", 5)
                .Component("elec_good", 3);

            // Chair, Pilot
            _builder.Create(RecipeType.ChairPilot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0301")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 5)
                .Component("lth_good", 4);

            // Altar, Evil
            _builder.Create(RecipeType.AltarEvil, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0055")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 6)
                .Component("lth_good", 3);

            // Illithid Table
            _builder.Create(RecipeType.IllithidTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0095")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 6)
                .Component("ref_plagionite", 3);

            // Desk, Control Center, Wide
            _builder.Create(RecipeType.DeskControlCenterWide, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0154")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_good", 6)
                .Component("ref_plagionite", 3);

            // Pot, Plant, Aloa
            _builder.Create(RecipeType.PotPlantAloa, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0187")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ancient_wood", 6)
                .Component("lth_good", 3);

            // Fountain, Stone, 4 Spouts
            _builder.Create(RecipeType.FountainStone4Spouts, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0246")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 5);

            // Carpet, Twisted Pattern
            _builder.Create(RecipeType.CarpetTwistedPattern, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0302")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 4);

            // DNA Extractor III
            _builder.Create(RecipeType.DNAExtractor3, SkillType.Fabrication)
                .Category(RecipeCategoryType.Tool)
                .Resref("dna_extractor_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_idailia", 3)
                .Component("elec_good", 2);
        }

        private void Tier4()
        {
            // Table, Stone, Small
            _builder.Create(RecipeType.TableStoneSmall, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0058")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 1)
                .Component("aracia_wood", 1);

            // Bed, Large
            _builder.Create(RecipeType.BedLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0075")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 1)
                .Component("ref_keromber", 1);

            // Desk, Corner /w Terminal
            _builder.Create(RecipeType.DeskCornerWithTerminal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0155")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 1)
                .Component("ref_keromber", 1);

            // Specimen Tube (Alien)
            _builder.Create(RecipeType.SpecimenTubeAlien, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0193")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 1)
                .Component("aracia_wood", 1);

            // Speaker, Standing
            _builder.Create(RecipeType.SpeakerStanding, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0272")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 1)
                .Component("elec_imperfect", 1);

            // Statue, Wizard
            _builder.Create(RecipeType.StatueWizard, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0012")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 2)
                .Component("fiberp_imperfect", 1);

            // Bench, Large
            _builder.Create(RecipeType.BenchLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0115")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 2)
                .Component("ref_keromber", 1);

            // Armchair, High Back (Orange)
            _builder.Create(RecipeType.ArmchairHighBackOrange, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0130")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 2)
                .Component("ref_keromber", 1);

            // Specimen Tube, Empty
            _builder.Create(RecipeType.SpecimenTubeEmpty, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0194")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1);

            // Locker, Open Shelves
            _builder.Create(RecipeType.LockerOpenShelves, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0273")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 2)
                .Component("fiberp_imperfect", 2);

            // Table, Stone, Large
            _builder.Create(RecipeType.TableStoneLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0073")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1);

            // Carpet, Round, Blue
            _builder.Create(RecipeType.CarpetRoundBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0054")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_imperfect", 2)
                .Component("lth_imperfect", 1);

            // Desk, Information/Control Center
            _builder.Create(RecipeType.DeskInformationControlCenter, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0156")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 2)
                .Component("ref_keromber", 1);

            // Specimen Tube, Tall
            _builder.Create(RecipeType.SpecimenTubeTall, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0195")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 2)
                .Component("ref_keromber", 1);

            // Table Dark Glass
            _builder.Create(RecipeType.TableDarkGlass, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0267")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_good", 4)
                .Component("ref_plagionite", 4);

            // Chandelier
            _builder.Create(RecipeType.Chandelier, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0065")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Foyer, Chandelier 
            _builder.Create(RecipeType.FoyerChandelier, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0247")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 5)
                .Component("fiberp_good", 3);


            // Chair, Wood, Medium
            _builder.Create(RecipeType.ChairWoodMedium, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0114")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 3)
                .Component("ref_keromber", 2);

            // Armchair, High Back (Blue)
            _builder.Create(RecipeType.ArmchairHighBackBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0131")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 3)
                .Component("ref_keromber", 2);

            // Table, Coffee, Elegant (White)
            _builder.Create(RecipeType.TableCoffeeElegantWhite, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0202")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 3)
                .Component("ref_keromber", 2);

            // Chair, Large (Grey/Red)
            _builder.Create(RecipeType.ChairLargeGreyRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0237")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("lth_good", 3)
                .Component("ref_plagionite", 2);

            // Statue, Huge
            _builder.Create(RecipeType.StatueHuge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0009")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Bench, Stone, Dwarven
            _builder.Create(RecipeType.BenchStoneDwarven, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0111")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Desk, Wall /w Terminal
            _builder.Create(RecipeType.DeskWallTerminal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0157")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 3)
                .Component("ref_keromber", 2);

            // Table, Conference, Centre Cloth
            _builder.Create(RecipeType.TableConferenceCentreCloth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0203")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 3)
                .Component("ref_keromber", 2);

            // Armchair, Low, Wood Trim (Blue)
            _builder.Create(RecipeType.CouchLeatherBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0230")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 3)
                .Component("ref_keromber", 2);

            // Cabinet, Liquor (Mixed)
            _builder.Create(RecipeType.CabinetLiquorMixed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0274")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 3)
                .Component("lth_imperfect", 3);

            // Console, Tall w/ Screen (Orange)
            _builder.Create(RecipeType.ConsoleTallwScreenOrange, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0303")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 3);

            // Pillar, Flame
            _builder.Create(RecipeType.PillarFlame, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0036")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2);

            // Drow Table
            _builder.Create(RecipeType.DrowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0108")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 4)
                .Component("ref_keromber", 2);

            // Desk, Wall /w Terminal, Wide
            _builder.Create(RecipeType.DeskWallTerminalWide, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0158")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 4)
                .Component("ref_keromber", 2);

            // Table, Oval, Centre Leg (Dark)
            _builder.Create(RecipeType.TableOvalCentreLegDark, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0204")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 4)
                .Component("ref_keromber", 2);

            // Chaise Lounge - Orange
            _builder.Create(RecipeType.ChaiseLoungeOrange, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0259")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("lth_good", 4)
                .Component("ref_plagionite", 2);

            // Chaise Lounge - Red
            _builder.Create(RecipeType.ChaiseLoungeRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0263")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("lth_good", 4)
                .Component("ref_plagionite", 2);

            // Holo Projector, Standing
            _builder.Create(RecipeType.HoloProjectorStanding, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0304")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 3);

            // Metal Wall, Single, Panels
            _builder.Create(RecipeType.MetalWallSinglePanels, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0325")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 2)
                .Component("fiberp_imperfect", 1);

            // Mining Well Platform
            _builder.Create(RecipeType.MiningWellPlatform, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0028")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2);

            // Weapon Rack, Wall Mounted
            _builder.Create(RecipeType.WeaponRackWallMounted, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0126")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2);

            // Work Station, Droid Repair
            _builder.Create(RecipeType.WorkStationDroidRepair, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0159")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 4)
                .Component("ref_keromber", 2);

            // Table, Oval, Low (Blue)
            _builder.Create(RecipeType.TableOvalLowBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0205")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 4)
                .Component("ref_keromber", 2);

            // Wall Light, Curved
            _builder.Create(RecipeType.WallLightCurved, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0211")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 4)
                .Component("ref_keromber", 2);

            // Metal Wall, Wide, Panels
            _builder.Create(RecipeType.MetalWallWidePanels, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0249")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 2)
                .Component("fiberp_imperfect", 2);

            // Counter, Lab (Straight)
            _builder.Create(RecipeType.CounterLabStraight, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0275")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 3);

            // Counter, Lab (Sink)
            _builder.Create(RecipeType.CounterLabSink, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0276")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 3);

            // Mining Well
            _builder.Create(RecipeType.MiningWell, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0010")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Bed, Stone, Yellow
            _builder.Create(RecipeType.BedStoneYellow, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0074")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Footlocker, Modern (Keyed Entry)
            _builder.Create(RecipeType.FootlockerModernKeyedEntry, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0160")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Table, Round, Low (Blue)
            _builder.Create(RecipeType.TableRoundLowBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0206")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 5)
                .Component("ref_keromber", 3);

            // Wall Light, Octagon
            _builder.Create(RecipeType.WallLightOctagon, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0212")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 5)
                .Component("ref_keromber", 3);

            // Metal Wall, Door, Panels
            _builder.Create(RecipeType.MetalWallDoorPanels, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0250")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 4)
                .Component("fiberp_imperfect", 2);

            // Metal Wall, Single, Indent
            _builder.Create(RecipeType.MetalWallSingleIndent, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0305")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("fiberp_imperfect", 2);

            // Mirror
            _builder.Create(RecipeType.Mirror, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0106")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Bed, Extra Large
            _builder.Create(RecipeType.BedExtraLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0052")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 5)
                .Component("ref_keromber", 3);

            // Fountain, Oval
            _builder.Create(RecipeType.FountainOval, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0161")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Table, Stone (Blue)
            _builder.Create(RecipeType.TableStoneBlue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0207")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 5)
                .Component("ref_keromber", 3);

            // Wardrobe, Curved (White)
            _builder.Create(RecipeType.WardrobeCurvedWhite, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0213")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 5)
                .Component("ref_keromber", 3);

            // Chair, Brown Wingback
            _builder.Create(RecipeType.ChairBrownWingback, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0277")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 4)
                .Component("lth_imperfect", 4);

            // Metal Wall, Wide, Indent
            _builder.Create(RecipeType.MetalWallWideIndent, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0306")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 3)
                .Component("fiberp_imperfect", 3);

            // Rune Pillar
            _builder.Create(RecipeType.RunePillar, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0104")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 3);

            // Statue, Cyric
            _builder.Create(RecipeType.StatueCyric, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0041")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 3);

            // Holo Display
            _builder.Create(RecipeType.HoloDisplay, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0162")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 6)
                .Component("ref_keromber", 3);

            // Table, Stone (Brown)
            _builder.Create(RecipeType.TableStoneBrown, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0208")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("aracia_wood", 6)
                .Component("ref_keromber", 3);

            // Washbasin, Lever Faucet
            _builder.Create(RecipeType.WashbasinLeverFaucet, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0214")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 3);

            // Metal Wall, Door, Indent
            _builder.Create(RecipeType.MetalWallDoorIndent, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0307")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 4)
                .Component("fiberp_imperfect", 3);

            // Pillar, Dish Tower
            _builder.Create(RecipeType.PillarDishTower, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0278")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 5);

            // DNA Extractor IV
            _builder.Create(RecipeType.DNAExtractor4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Tool)
                .Resref("dna_extractor_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_barinium", 3)
                .Component("elec_imperfect", 2);
        }

        private void Tier5()
        {

            // Sphinx Statue
            _builder.Create(RecipeType.SphinxStatue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0027")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 1)
                .Component("hyphae_wood", 1);

            // Dartboard
            _builder.Create(RecipeType.Dartboard, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0098")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 1)
                .Component("lth_high", 1);

            // Holo Display 2
            _builder.Create(RecipeType.HoloDisplay2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0163")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 1)
                .Component("ref_jasioclase", 1);

            // Statue, Bust on Column
            _builder.Create(RecipeType.StatueBustOnColumn, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0196")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 1)
                .Component("hyphae_wood", 1);

            // Fridge, Stainless
            _builder.Create(RecipeType.FridgeStainless, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0218")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 2)
                .Component("elec_high", 1);

            // Metal Wall, Single - Light, White
            _builder.Create(RecipeType.MetalWallSingleLight, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0251")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 2)
                .Component("fiberp_high", 1);

            // Holo Display 1
            _builder.Create(RecipeType.HoloDisplay1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0308")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 1)
                .Component("elec_high", 1);

            // Dran Statue
            _builder.Create(RecipeType.DranStatue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0048")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);

            // Map
            _builder.Create(RecipeType.Map, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0099")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("fiberp_high", 2)
                .Component("lth_high", 1);

            // Holo Display 4
            _builder.Create(RecipeType.HoloDisplay4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0164")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 2)
                .Component("ref_jasioclase", 1);

            // Statue, Kneeling Man
            _builder.Create(RecipeType.StatueKneelingMan, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0197")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);

            // Shelves, Warehouse, Full
            _builder.Create(RecipeType.ShelvesWarehouseFull, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0242")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("fiberp_high", 4);

            // Sea Idol
            _builder.Create(RecipeType.SeaIdol, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0042")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 2)
                .Component("fiberp_high", 1);

            // Painting 2
            _builder.Create(RecipeType.Painting2, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0089")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 2)
                .Component("ref_jasioclase", 1);

            // Holo Display 5
            _builder.Create(RecipeType.HoloDisplay5, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0165")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 2)
                .Component("ref_jasioclase", 1);

            // Statue, Robed Figure /w Staff
            _builder.Create(RecipeType.StatueRobedFigureWithStaff, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0198")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);

            // Bookshelf, Jedi
            _builder.Create(RecipeType.BookshelfJedi, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0243")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 4);

            // Metal Wall, Wide - Light, White
            _builder.Create(RecipeType.MetalWallWideLight, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0235")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("fiberp_high", 2);

            // Chair, Stone
            _builder.Create(RecipeType.ChairStone, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0122")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Drow Altar
            _builder.Create(RecipeType.DrowAltar, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0097")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Holo Projector 1
            _builder.Create(RecipeType.HoloProjector1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0166")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 3)
                .Component("ref_jasioclase", 2);

            // Statue, Senator
            _builder.Create(RecipeType.StatueSenator, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0199")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Banner, Jedi Order
            _builder.Create(RecipeType.BannerJediOrder, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0317")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("lth_high", 5)
                .Component("fiberp_high", 5);

            // Banner, Empire
            _builder.Create(RecipeType.BannerEmpire, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0318")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("lth_high", 5)
                .Component("fiberp_high", 5);

            // Banner, Mandalorian
            _builder.Create(RecipeType.BannerMandalorian, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0319")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("lth_high", 5)
                .Component("fiberp_high", 5);

            // Banner, Republic
            _builder.Create(RecipeType.BannerRepublic, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0320")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("lth_high", 5)
                .Component("fiberp_high", 5);

            // Banner, Cartel
            _builder.Create(RecipeType.BannerCartel, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0321")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("lth_high", 5)
                .Component("fiberp_high", 5);

            // Throne, Wood
            _builder.Create(RecipeType.ThroneWood, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0121")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 3)
                .Component("ref_jasioclase", 2);

            // Illithid Chair
            _builder.Create(RecipeType.IllithidChair, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0112")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 3)
                .Component("ref_jasioclase", 2);

            // Holo Projector 2
            _builder.Create(RecipeType.HoloProjector2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0167")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 3)
                .Component("ref_jasioclase", 2);

            // Storage Tank, Hemisphere /w Monitor
            _builder.Create(RecipeType.StorageTankHemisphereWithMonitor, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0200")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 3)
                .Component("ref_jasioclase", 2);

            // Wall Metal, Door Light 
            _builder.Create(RecipeType.MetalWallDoorLight, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0248")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 3);

            // Console, Tall w/ Screen (Spiral)
            _builder.Create(RecipeType.ConsoleTwScreenSpiral, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0279")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 3);

            // Metal Wall, Single, Grating
            _builder.Create(RecipeType.MetalWallSingleGrating, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0309")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("fiberp_high", 3);

            // Dance Floor
            _builder.Create(RecipeType.DanceFloor, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0314")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 4)
                .Component("ref_arkoxit", 1);

            // Monster Statue
            _builder.Create(RecipeType.MonsterStatue, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0044")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("hyphae_wood", 2);

            // Shrine of Umberlee
            _builder.Create(RecipeType.ShrineOfUmberlee, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0103")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("hyphae_wood", 2);

            // Instrument Panel, Large Monitor (Technical Data)
            _builder.Create(RecipeType.InstrumentPanelLargeMonitorTechnicalData, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0168")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 4)
                .Component("ref_jasioclase", 2);

            // Storage Tank, Cylinder
            _builder.Create(RecipeType.StorageTankCylinder, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0280")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 4);

            // WorkStationMonitors
            _builder.Create(RecipeType.WorkStationMonitors, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0281")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("elec_high", 3);

            // Metal Wall, Wide, Grating
            _builder.Create(RecipeType.MetalWallWideGrating, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0310")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 3);

            // Carpet, Fancy, Smaller
            _builder.Create(RecipeType.CarpetFancySmaller, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0096")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2);

            // Drow Chair
            _builder.Create(RecipeType.DrowChair, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0116")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 4)
                .Component("ref_jasioclase", 2);

            // Kolto Tank (Empty)
            _builder.Create(RecipeType.KoltoTankEmpty, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0169")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 4)
                .Component("ref_jasioclase", 2);

            // Chest, Lengthwise
            _builder.Create(RecipeType.ChestLengthwise, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0282")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 4)
                .Component("ref_jasioclase", 3);

            // Metal Wall, Door, Grating
            _builder.Create(RecipeType.MetalWallDoorGrating, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0311")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 4);

            // Metal Wall, Single, Hazard
            _builder.Create(RecipeType.MetalWallSingleHazard, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0322")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5)
                .Component("fiberp_high", 5);

            // Bench, Wood, Small 2
            _builder.Create(RecipeType.BenchWoodSmall2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0123")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("ref_jasioclase", 3);

            // Drow Bar
            _builder.Create(RecipeType.DrowBar, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0102")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("ref_jasioclase", 3);

            // Lamp, Eggs (Pink)
            _builder.Create(RecipeType.LampEggsPink, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0170")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 5)
                .Component("ref_jasioclase", 3);

            // Television, Big Screen
            _builder.Create(RecipeType.TelevisionBigScreen, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0245")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 4)
                .Component("elec_high", 6);

            // Console, Central Control
            _builder.Create(RecipeType.ConsoleCentralControl, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0283")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 5);

            // Medical, Monitoring Unit
            _builder.Create(RecipeType.MedicalMonitoringUnit, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0313")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 5)
                .Component("ref_arkoxit", 2);

            // Metal Wall, Wide, Hazard
            _builder.Create(RecipeType.MetalWallWideHazard, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0323")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5)
                .Component("fiberp_high", 5);

            // Chair, Shell
            _builder.Create(RecipeType.ChairShell, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0124")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("ref_jasioclase", 3);

            // Bench, Wood, Large
            _builder.Create(RecipeType.BenchWoodLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0117")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 5)
                .Component("ref_jasioclase", 3);

            // Lamp, On Poles
            _builder.Create(RecipeType.LampOnPoles, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0171")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("elec_imperfect", 6)
                .Component("ref_jasioclase", 3);

            // Couch, Blanket Cover, Red
            _builder.Create(RecipeType.CouchBlanketCoverRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0260")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 6)
                .Component("ref_jasioclase", 3);

            // Console, Circular Command
            _builder.Create(RecipeType.ConsoleCircularCommand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0284")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 5);

            // Holo Projector, Hover
            _builder.Create(RecipeType.HoloProjectorHover, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0315")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("elec_high", 5)
                .Component("ref_arkoxit", 2);

            // Metal Wall, Door, Hazard
            _builder.Create(RecipeType.MetalWallDoorHazard, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0324")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5)
                .Component("fiberp_high", 5);

            // Storage, Warehouse, Industry
            _builder.Create(RecipeType.StorageWarehouseIndustry, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0316")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 5)
                .Component("ref_arkoxit", 2);

            // Couch, Wood, Yellow
            _builder.Create(RecipeType.CouchWoodYellow, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0125")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("hyphae_wood", 6)
                .Component("lth_high", 3);

            // Lantern, Post, Marble
            _builder.Create(RecipeType.LanternPostMarble, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0172")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 6)
                .Component("hyphae_wood", 3);

            // Locker, Metal Trapezoid
            _builder.Create(RecipeType.LockerMetalTrapezoid, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0173")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 6)
                .Component("hyphae_wood", 3);

            // Storage, Cargo Container
            _builder.Create(RecipeType.StorageCargoContainer, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0285")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 6)
                .Component("hyphae_wood", 4);

            // Console, Corner, Large
            _builder.Create(RecipeType.ConsoleCornerLarge, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0286")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 6)
                .Component("elec_high", 6);

            // Holo Projector, Small Tree
            _builder.Create(RecipeType.HoloProjectorSmallTree, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0312")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_jasioclase", 6)
                .Component("elec_high", 5);

            // Droid Assembly Terminal
            _builder.Create(RecipeType.DroidAssemblyTerminal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0269")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .Component("ref_jasioclase", 5)
                .Component("elec_imperfect", 3);

            // Beast Stables Terminal
            _builder.Create(RecipeType.BeastStablesTerminal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0270")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .Component("ref_jasioclase", 5)
                .Component("elec_imperfect", 3);

            // Incubator
            _builder.Create(RecipeType.Incubator, SkillType.Fabrication)
                .Category(RecipeCategoryType.Crafting)
                .Resref("structure_0271")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .Component("zinsiam", 6)
                .Component("elec_imperfect", 6)
                .Component("diamond", 2);

            // DNA Extractor V
            _builder.Create(RecipeType.DNAExtractor5, SkillType.Fabrication)
                .Category(RecipeCategoryType.Tool)
                .Resref("dna_extractor_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("ref_gostian", 3)
                .Component("elec_high", 2);

            // Swoop Bike, Black
            _builder.Create(RecipeType.SwoopBikeBlack, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0326")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("r_pow_supp_unit", 2)
                .Component("r_const_parts", 5)
                .Component("em_amp_4", 2)
                .Component("hull_boost_4", 2)
                .Component("elec_high", 40)
                .Component("ref_arkoxit", 20)
                .Component("emerald", 10)
                .Component("zinsiam", 5);

            // Swoop Bike, Grey
            _builder.Create(RecipeType.SwoopBikeGrey, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0327")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("r_pow_supp_unit", 2)
                .Component("r_const_parts", 5)
                .Component("em_amp_4", 2)
                .Component("hull_boost_4", 2)
                .Component("elec_high", 40)
                .Component("ref_arkoxit", 20)
                .Component("emerald", 10)
                .Component("zinsiam", 5);

            // Swoop Bike, Red
            _builder.Create(RecipeType.SwoopBikeRed, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0328")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("r_pow_supp_unit", 2)
                .Component("r_const_parts", 5)
                .Component("em_amp_4", 2)
                .Component("hull_boost_4", 2)
                .Component("elec_high", 40)
                .Component("ref_arkoxit", 20)
                .Component("emerald", 10)
                .Component("zinsiam", 5);

            // Swoop Bike, Yellow
            _builder.Create(RecipeType.SwoopBikeYellow, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0329")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.FurnitureBlueprints, 5)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 2)
                .Component("r_pow_supp_unit", 2)
                .Component("r_const_parts", 5)
                .Component("em_amp_4", 2)
                .Component("hull_boost_4", 2)
                .Component("elec_high", 40)
                .Component("ref_arkoxit", 20)
                .Component("emerald", 10)
                .Component("zinsiam", 5);

        }
    }
}
