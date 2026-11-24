using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;

public class SwtorFurnishingRecipes : IRecipeListDefinition
{
    private readonly RecipeBuilder _builder = new();

    public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
    {
        Bed();
        MiscellaneousFurniture();
        Wall();
        Electronics();
        Fixtures();
        Surfaces();
        Seating();
        Statues();
        Flooring();
        Doors();
        Lighting();

        return _builder.Build();
    }

    private void Bed()
    {
        _builder.Create(RecipeType.SwtorBedBunkCream, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0437")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 4)
            .FabricatorParameters(frameSizePercent: 57, shellSizePercent: 28, frameQualityPercent: 60, shellQualityPercent: 23, wearRate: 4);

        _builder.Create(RecipeType.SwtorBedBunkOpen, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0438")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 4)
            .FabricatorParameters(frameSizePercent: 51, shellSizePercent: 31, frameQualityPercent: 57, shellQualityPercent: 24, wearRate: 5);

        _builder.Create(RecipeType.SwtorBedBunkShipDark, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0439")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 4)
            .FabricatorParameters(frameSizePercent: 57, shellSizePercent: 27, frameQualityPercent: 63, shellQualityPercent: 22, wearRate: 4);

        _builder.Create(RecipeType.SwtorBedBunkShipLight, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0440")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 4)
            .FabricatorParameters(frameSizePercent: 55, shellSizePercent: 29, frameQualityPercent: 55, shellQualityPercent: 28, wearRate: 4);

        _builder.Create(RecipeType.SwtorBedCreamSingle, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0441")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 2)
            .Component("fiberp_ruined", 1)
            .FabricatorParameters(frameSizePercent: 51, shellSizePercent: 27, frameQualityPercent: 55, shellQualityPercent: 27, wearRate: 2);

        _builder.Create(RecipeType.SwtorBedDoubleBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0442")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 54, shellSizePercent: 27, frameQualityPercent: 56, shellQualityPercent: 24, wearRate: 4);

        _builder.Create(RecipeType.SwtorBedDoubleBronze, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0443")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 32, frameQualityPercent: 55, shellQualityPercent: 30, wearRate: 4);

        _builder.Create(RecipeType.SwtorBedDoubleOrange, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0444")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 32, frameQualityPercent: 55, shellQualityPercent: 21, wearRate: 3);

        _builder.Create(RecipeType.SwtorBedDoublePurple, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0445")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 58, shellSizePercent: 26, frameQualityPercent: 57, shellQualityPercent: 28, wearRate: 5);

        _builder.Create(RecipeType.SwtorBedGreyStripeDouble, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0446")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 56, shellSizePercent: 27, frameQualityPercent: 59, shellQualityPercent: 24, wearRate: 5);

        _builder.Create(RecipeType.SwtorBedJediStone, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0447")
            .Level(38)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 5)
            .Component("aracia_wood", 3)
            .FabricatorParameters(frameSizePercent: 58, shellSizePercent: 27, frameQualityPercent: 55, shellQualityPercent: 27, wearRate: 8);

        _builder.Create(RecipeType.SwtorBedMandalorian, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0448")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 54, shellSizePercent: 30, frameQualityPercent: 62, shellQualityPercent: 20, wearRate: 3);

        _builder.Create(RecipeType.SwtorBedMetalPost, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0449")
            .Level(14)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 3)
            .Component("ref_scordspar", 2)
            .FabricatorParameters(frameSizePercent: 55, shellSizePercent: 29, frameQualityPercent: 64, shellQualityPercent: 21, wearRate: 6);

        _builder.Create(RecipeType.SwtorBedWood, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0450")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 2)
            .Component("fiberp_good", 1)
            .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 26, frameQualityPercent: 57, shellQualityPercent: 25, wearRate: 8);
    }

    private void MiscellaneousFurniture()
    {
        _builder.Create(RecipeType.SwtorAmmoBox, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0451")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 30, frameQualityPercent: 38, shellQualityPercent: 28, wearRate: 7);

        _builder.Create(RecipeType.SwtorArmory001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0453")
            .Level(42)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 31, frameQualityPercent: 31, shellQualityPercent: 36, wearRate: 7);

        _builder.Create(RecipeType.SwtorArmory002, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0454")
            .Level(42)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 33, frameQualityPercent: 39, shellQualityPercent: 28, wearRate: 7);

        _builder.Create(RecipeType.SwtorAwningEntranceBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0457")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 34, frameQualityPercent: 36, shellQualityPercent: 34, wearRate: 3);

        _builder.Create(RecipeType.SwtorAwningWallHangingBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0458")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 28, frameQualityPercent: 36, shellQualityPercent: 37, wearRate: 2);

        _builder.Create(RecipeType.SwtorAwningWallHangingOrange, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0459")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 33, frameQualityPercent: 32, shellQualityPercent: 34, wearRate: 3);

        _builder.Create(RecipeType.SwtorBackpack001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0460")
            .Level(5)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 3)
            .Component("fiberp_ruined", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 28, frameQualityPercent: 34, shellQualityPercent: 29, wearRate: 4);

        _builder.Create(RecipeType.SwtorBarrel001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0477")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 33, frameQualityPercent: 37, shellQualityPercent: 30, wearRate: 3);

        _builder.Create(RecipeType.SwtorBarrel002Stack, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0478")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 36, frameQualityPercent: 29, shellQualityPercent: 32, wearRate: 4);

        _builder.Create(RecipeType.SwtorBarrel003, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0479")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 37, frameQualityPercent: 35, shellQualityPercent: 38, wearRate: 3);

        _builder.Create(RecipeType.SwtorBottle001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0488")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 31, frameQualityPercent: 35, shellQualityPercent: 38, wearRate: 7);

        _builder.Create(RecipeType.SwtorBottle002, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0489")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 33, frameQualityPercent: 31, shellQualityPercent: 34, wearRate: 5);

        _builder.Create(RecipeType.SwtorBurlapSack001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0491")
            .Level(5)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 3)
            .Component("fiberp_ruined", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 38, frameQualityPercent: 36, shellQualityPercent: 30, wearRate: 2);

        _builder.Create(RecipeType.SwtorBurlapSack002, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0492")
            .Level(5)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 3)
            .Component("fiberp_ruined", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 35, frameQualityPercent: 35, shellQualityPercent: 37, wearRate: 2);

        _builder.Create(RecipeType.SwtorBusStop, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0493")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 38, frameQualityPercent: 33, shellQualityPercent: 38, wearRate: 2);

        _builder.Create(RecipeType.SwtorCanopyHanging, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0494")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 29, frameQualityPercent: 39, shellQualityPercent: 38, wearRate: 4);

        _builder.Create(RecipeType.SwtorCanopy001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0495")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 30, frameQualityPercent: 31, shellQualityPercent: 33, wearRate: 3);

        _builder.Create(RecipeType.SwtorCanopy002, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0496")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 37, frameQualityPercent: 29, shellQualityPercent: 33, wearRate: 4);

        _builder.Create(RecipeType.SwtorCanopy003, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0497")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 28, frameQualityPercent: 29, shellQualityPercent: 34, wearRate: 4);

        _builder.Create(RecipeType.SwtorCanopy005, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0498")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 33, frameQualityPercent: 39, shellQualityPercent: 37, wearRate: 4);

        _builder.Create(RecipeType.SwtorCargoShelfEmpty, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0501")
            .Level(42)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 29, frameQualityPercent: 36, shellQualityPercent: 33, wearRate: 9);

        _builder.Create(RecipeType.SwtorCargoShelf01, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0502")
            .Level(42)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 32, frameQualityPercent: 31, shellQualityPercent: 28, wearRate: 8);

        _builder.Create(RecipeType.SwtorCargoStack1, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0503")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 28, frameQualityPercent: 32, shellQualityPercent: 35, wearRate: 7);

        _builder.Create(RecipeType.SwtorCargoStack2, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0504")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 35, frameQualityPercent: 39, shellQualityPercent: 35, wearRate: 6);

        _builder.Create(RecipeType.SwtorContainerPanelAccess, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0530")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 35, frameQualityPercent: 33, shellQualityPercent: 34, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer1, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0531")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 30, frameQualityPercent: 37, shellQualityPercent: 30, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer2Stack, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0532")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 36, frameQualityPercent: 32, shellQualityPercent: 37, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer3, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0533")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 37, frameQualityPercent: 31, shellQualityPercent: 35, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer4, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0534")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 30, frameQualityPercent: 38, shellQualityPercent: 35, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer5Stack, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0535")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 33, frameQualityPercent: 33, shellQualityPercent: 29, wearRate: 6);

        _builder.Create(RecipeType.SwtorContainer6, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0536")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 32, frameQualityPercent: 30, shellQualityPercent: 28, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer7Stack, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0537")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 37, frameQualityPercent: 39, shellQualityPercent: 37, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer8, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0538")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 37, frameQualityPercent: 38, shellQualityPercent: 36, wearRate: 7);

        _builder.Create(RecipeType.SwtorContainer9, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0539")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 28, frameQualityPercent: 31, shellQualityPercent: 32, wearRate: 6);

        _builder.Create(RecipeType.SwtorCrate03, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0546")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 28, frameQualityPercent: 36, shellQualityPercent: 38, wearRate: 7);

        _builder.Create(RecipeType.SwtorCrate04, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0547")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 30, frameQualityPercent: 37, shellQualityPercent: 35, wearRate: 7);

        _builder.Create(RecipeType.SwtorCratePileRep1, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0548")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 32, frameQualityPercent: 39, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorCratePileRep2, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0549")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 33, frameQualityPercent: 37, shellQualityPercent: 32, wearRate: 6);

        _builder.Create(RecipeType.SwtorCratePileRep3, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0550")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 28, frameQualityPercent: 37, shellQualityPercent: 36, wearRate: 7);

        _builder.Create(RecipeType.SwtorCratePileRep4, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0551")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 36, frameQualityPercent: 34, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorCup, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0554")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 38, frameQualityPercent: 35, shellQualityPercent: 37, wearRate: 6);

        _builder.Create(RecipeType.SwtorFlower001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0621")
            .Level(26)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 4)
            .Component("lth_good", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 31, frameQualityPercent: 33, shellQualityPercent: 32, wearRate: 6);

        _builder.Create(RecipeType.SwtorFlower002, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0622")
            .Level(26)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 4)
            .Component("lth_good", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 29, frameQualityPercent: 35, shellQualityPercent: 33, wearRate: 8);

        _builder.Create(RecipeType.SwtorGSIDropBox001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0633")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 36, frameQualityPercent: 36, shellQualityPercent: 38, wearRate: 6);

        _builder.Create(RecipeType.SwtorGunRack, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0635")
            .Level(2)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 29, frameQualityPercent: 35, shellQualityPercent: 28, wearRate: 4);

        _builder.Create(RecipeType.SwtorImperialMedicalShed, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0652")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 36, frameQualityPercent: 34, shellQualityPercent: 33, wearRate: 4);

        _builder.Create(RecipeType.SwtorLockerGreen, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0675")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 32, frameQualityPercent: 33, shellQualityPercent: 38, wearRate: 6);

        _builder.Create(RecipeType.SwtorLockerGrey, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0676")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 31, frameQualityPercent: 31, shellQualityPercent: 29, wearRate: 6);

        _builder.Create(RecipeType.SwtorLockerOrange, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0677")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 34, frameQualityPercent: 37, shellQualityPercent: 35, wearRate: 7);

        _builder.Create(RecipeType.SwtorMailBox, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0678")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 36, frameQualityPercent: 33, shellQualityPercent: 31, wearRate: 7);

        _builder.Create(RecipeType.SwtorMarketItem1, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0679")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 30, frameQualityPercent: 33, shellQualityPercent: 32, wearRate: 6);

        _builder.Create(RecipeType.SwtorMarketItem2, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0680")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 30, frameQualityPercent: 34, shellQualityPercent: 35, wearRate: 5);

        _builder.Create(RecipeType.SwtorPickaxe1, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0696")
            .Level(2)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 36, frameQualityPercent: 36, shellQualityPercent: 36, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlanterPlasteel, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0747")
            .Level(21)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 1)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 35, frameQualityPercent: 29, shellQualityPercent: 38, wearRate: 6);

        _builder.Create(RecipeType.SwtorPlanterWhiteCurve, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0748")
            .Level(21)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 1)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 38, frameQualityPercent: 33, shellQualityPercent: 35, wearRate: 7);

        _builder.Create(RecipeType.SwtorPlanterWhiteEndcap, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0749")
            .Level(21)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 1)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 32, frameQualityPercent: 36, shellQualityPercent: 35, wearRate: 7);

        _builder.Create(RecipeType.SwtorPlanterWhiteFullCurve, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0750")
            .Level(21)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 1)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 33, frameQualityPercent: 34, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorPlanterWhiteStraight, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0751")
            .Level(21)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 1)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 33, frameQualityPercent: 33, shellQualityPercent: 38, wearRate: 6);

        _builder.Create(RecipeType.SwtorPlanterWhiteVase, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0752")
            .Level(21)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 1)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 33, frameQualityPercent: 35, shellQualityPercent: 36, wearRate: 7);

        _builder.Create(RecipeType.SwtorPlanter1, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0753")
            .Level(21)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 1)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 36, frameQualityPercent: 35, shellQualityPercent: 36, wearRate: 6);

        _builder.Create(RecipeType.SwtorSaberRemote, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0794")
            .Level(4)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_ruined", 3)
            .Component("lth_ruined", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 30, frameQualityPercent: 38, shellQualityPercent: 34, wearRate: 3);

        _builder.Create(RecipeType.SwtorSafeBox, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0795")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 36, frameQualityPercent: 31, shellQualityPercent: 38, wearRate: 7);

        _builder.Create(RecipeType.SwtorSalvagePack, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0796")
            .Level(5)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 3)
            .Component("fiberp_ruined", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 36, frameQualityPercent: 32, shellQualityPercent: 31, wearRate: 2);

        _builder.Create(RecipeType.SwtorSandbag001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0797")
            .Level(5)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 3)
            .Component("fiberp_ruined", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 38, frameQualityPercent: 33, shellQualityPercent: 34, wearRate: 2);

        _builder.Create(RecipeType.SwtorSandbag002, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0798")
            .Level(5)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 3)
            .Component("fiberp_ruined", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 31, frameQualityPercent: 39, shellQualityPercent: 38, wearRate: 4);

        _builder.Create(RecipeType.SwtorSlotmachine6Dark, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0813")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 29, frameQualityPercent: 29, shellQualityPercent: 28, wearRate: 5);

        _builder.Create(RecipeType.SwtorStimpack, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0824")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("lth_good", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 35, frameQualityPercent: 39, shellQualityPercent: 32, wearRate: 6);

        _builder.Create(RecipeType.SwtorTool, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0854")
            .Level(2)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 35, frameQualityPercent: 34, shellQualityPercent: 37, wearRate: 4);

        _builder.Create(RecipeType.SwtorToolBox, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0855")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 37, frameQualityPercent: 37, shellQualityPercent: 28, wearRate: 6);

        _builder.Create(RecipeType.SwtorToy, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0857")
            .Level(4)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_ruined", 3)
            .Component("lth_ruined", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 37, frameQualityPercent: 36, shellQualityPercent: 36, wearRate: 4);

        _builder.Create(RecipeType.SwtorTrashCan, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0862")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 33, frameQualityPercent: 37, shellQualityPercent: 37, wearRate: 5);

        _builder.Create(RecipeType.SwtorTrashCan02, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0863")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 35, frameQualityPercent: 37, shellQualityPercent: 34, wearRate: 3);

        _builder.Create(RecipeType.SwtorVendorCanopy, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0884")
            .Level(8)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("lth_ruined", 4)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 36, frameQualityPercent: 34, shellQualityPercent: 28, wearRate: 2);

        _builder.Create(RecipeType.SwtorWeaponsCrate001, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0939")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 30, frameQualityPercent: 36, shellQualityPercent: 28, wearRate: 6);

        _builder.Create(RecipeType.SwtorWeaponsCrate002, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0940")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 37, frameQualityPercent: 29, shellQualityPercent: 29, wearRate: 7);
    }

    private void Wall()
    {
        _builder.Create(RecipeType.SwtorAnimalPen, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0452")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 48, frameQualityPercent: 26, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorBarricade001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0480")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 46, frameQualityPercent: 30, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorBarricade002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0481")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 49, frameQualityPercent: 32, shellQualityPercent: 53, wearRate: 3);

        _builder.Create(RecipeType.SwtorBarricade003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0482")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 45, frameQualityPercent: 25, shellQualityPercent: 51, wearRate: 3);

        _builder.Create(RecipeType.SwtorFence, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0572")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 49, frameQualityPercent: 27, shellQualityPercent: 50, wearRate: 4);

        _builder.Create(RecipeType.SwtorFenceClosedDoor, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0573")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 51, frameQualityPercent: 28, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorFenceOpenDoor, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0574")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 51, frameQualityPercent: 34, shellQualityPercent: 50, wearRate: 3);

        _builder.Create(RecipeType.SwtorFencePillar, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0575")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 49, frameQualityPercent: 25, shellQualityPercent: 54, wearRate: 4);

        _builder.Create(RecipeType.SwtorFencePowered, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0576")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 49, frameQualityPercent: 29, shellQualityPercent: 50, wearRate: 5);

        _builder.Create(RecipeType.SwtorFenceUnPowered1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0577")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 48, frameQualityPercent: 26, shellQualityPercent: 58, wearRate: 5);

        _builder.Create(RecipeType.SwtorFenceUnPowered2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0578")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 45, frameQualityPercent: 27, shellQualityPercent: 56, wearRate: 3);

        _builder.Create(RecipeType.SwtorFenceUnPowered3, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0579")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 46, frameQualityPercent: 29, shellQualityPercent: 52, wearRate: 4);

        _builder.Create(RecipeType.SwtorFenceWideDoor, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0580")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 51, frameQualityPercent: 29, shellQualityPercent: 53, wearRate: 5);

        _builder.Create(RecipeType.SwtorFenceWideDoorClosed, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0581")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 49, frameQualityPercent: 33, shellQualityPercent: 51, wearRate: 4);

        _builder.Create(RecipeType.SwtorFenceAngularLargeLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0582")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 54, frameQualityPercent: 33, shellQualityPercent: 52, wearRate: 3);

        _builder.Create(RecipeType.SwtorFenceAngularLargeShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0583")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 52, frameQualityPercent: 28, shellQualityPercent: 51, wearRate: 5);

        _builder.Create(RecipeType.SwtorFenceAngularSmallLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0584")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 51, frameQualityPercent: 28, shellQualityPercent: 51, wearRate: 4);

        _builder.Create(RecipeType.SwtorFenceAngularSmallShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0585")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 53, frameQualityPercent: 25, shellQualityPercent: 51, wearRate: 5);

        _builder.Create(RecipeType.SwtorFenceGeometricLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0586")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 51, frameQualityPercent: 26, shellQualityPercent: 59, wearRate: 3);

        _builder.Create(RecipeType.SwtorFenceGeometricShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0587")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 46, frameQualityPercent: 33, shellQualityPercent: 52, wearRate: 5);

        _builder.Create(RecipeType.SwtorFenceLightposts, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0588")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 46, frameQualityPercent: 27, shellQualityPercent: 54, wearRate: 3);

        _builder.Create(RecipeType.SwtorFenceRailing, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0589")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 49, frameQualityPercent: 25, shellQualityPercent: 56, wearRate: 3);

        _builder.Create(RecipeType.SwtorFenceSlatBarrier, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0590")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 49, frameQualityPercent: 28, shellQualityPercent: 53, wearRate: 3);

        _builder.Create(RecipeType.SwtorPillarAngledBrace, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0697")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 48, frameQualityPercent: 31, shellQualityPercent: 51, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillarCurved, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0698")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 52, frameQualityPercent: 31, shellQualityPercent: 52, wearRate: 6);

        _builder.Create(RecipeType.SwtorPillarExteriorImp, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0699")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 45, frameQualityPercent: 25, shellQualityPercent: 53, wearRate: 4);

        _builder.Create(RecipeType.SwtorPillarExteriorImpGate, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0700")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 50, frameQualityPercent: 30, shellQualityPercent: 53, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillarExteriorImpTurret, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0701")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 52, frameQualityPercent: 28, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillarExteriorRep, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0702")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 49, frameQualityPercent: 30, shellQualityPercent: 53, wearRate: 4);

        _builder.Create(RecipeType.SwtorPillarGreyTechArch, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0703")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 50, frameQualityPercent: 32, shellQualityPercent: 51, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillarGreyTechBrace, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0704")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 49, frameQualityPercent: 28, shellQualityPercent: 57, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillarRedStripe, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0705")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 47, frameQualityPercent: 32, shellQualityPercent: 52, wearRate: 6);

        _builder.Create(RecipeType.SwtorPillarRedVents, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0706")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 53, frameQualityPercent: 33, shellQualityPercent: 51, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillarRepublicInterior, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0707")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 54, frameQualityPercent: 31, shellQualityPercent: 51, wearRate: 4);

        _builder.Create(RecipeType.SwtorPillarRoundDurasteel, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0708")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 50, frameQualityPercent: 30, shellQualityPercent: 54, wearRate: 6);

        _builder.Create(RecipeType.SwtorPillarTatooineFull, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0709")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 47, frameQualityPercent: 25, shellQualityPercent: 54, wearRate: 6);

        _builder.Create(RecipeType.SwtorPillarTatooineHalf, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0710")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 49, frameQualityPercent: 34, shellQualityPercent: 50, wearRate: 6);

        _builder.Create(RecipeType.SwtorPillarYellowTrim, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0711")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 48, frameQualityPercent: 32, shellQualityPercent: 50, wearRate: 6);

        _builder.Create(RecipeType.SwtorPillar001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0712")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 49, frameQualityPercent: 25, shellQualityPercent: 57, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillar002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0713")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 49, frameQualityPercent: 25, shellQualityPercent: 56, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillar003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0714")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 52, frameQualityPercent: 25, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillar004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0715")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 48, frameQualityPercent: 32, shellQualityPercent: 53, wearRate: 4);

        _builder.Create(RecipeType.SwtorPillar005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0716")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 47, frameQualityPercent: 31, shellQualityPercent: 53, wearRate: 4);

        _builder.Create(RecipeType.SwtorPillar006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0717")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 53, frameQualityPercent: 25, shellQualityPercent: 57, wearRate: 4);

        _builder.Create(RecipeType.SwtorPillar007, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0718")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 45, frameQualityPercent: 29, shellQualityPercent: 53, wearRate: 4);

        _builder.Create(RecipeType.SwtorPillar008, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0719")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 50, frameQualityPercent: 27, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillar009, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0720")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 47, frameQualityPercent: 30, shellQualityPercent: 52, wearRate: 5);

        _builder.Create(RecipeType.SwtorPillar010, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0721")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("ref_scordspar", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 48, frameQualityPercent: 31, shellQualityPercent: 51, wearRate: 6);

        _builder.Create(RecipeType.SwtorTankTrap001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0848")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 54, frameQualityPercent: 27, shellQualityPercent: 58, wearRate: 3);

        _builder.Create(RecipeType.SwtorTankTrap002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0849")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 45, frameQualityPercent: 33, shellQualityPercent: 52, wearRate: 3);

        _builder.Create(RecipeType.SwtorVentFloorAngled, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0891")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 49, frameQualityPercent: 26, shellQualityPercent: 58, wearRate: 4);

        _builder.Create(RecipeType.SwtorVentWallPanel, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0892")
            .Level(12)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 2)
            .Component("fiberp_flawed", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 50, frameQualityPercent: 30, shellQualityPercent: 51, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallExteriorImpCorner, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0893")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 45, frameQualityPercent: 28, shellQualityPercent: 53, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallExteriorImpLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0894")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 47, frameQualityPercent: 28, shellQualityPercent: 51, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallExteriorImpShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0895")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 46, frameQualityPercent: 29, shellQualityPercent: 56, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallExteriorPipesGreyCorner, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0896")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 52, frameQualityPercent: 26, shellQualityPercent: 53, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallExteriorPipesGreyLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0897")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 55, frameQualityPercent: 35, shellQualityPercent: 50, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallExteriorPipesGreyShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0898")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 46, frameQualityPercent: 29, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallExteriorPipesTanCorner, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0899")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 48, frameQualityPercent: 27, shellQualityPercent: 58, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallExteriorPipesTanGate, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0900")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 45, frameQualityPercent: 33, shellQualityPercent: 51, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallExteriorPipesTanLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0901")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 49, frameQualityPercent: 33, shellQualityPercent: 51, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallExteriorPipesTanShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0902")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 45, frameQualityPercent: 25, shellQualityPercent: 60, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallExteriorRepCorner, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0903")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 49, frameQualityPercent: 30, shellQualityPercent: 55, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallExteriorRepGate, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0904")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 48, frameQualityPercent: 31, shellQualityPercent: 52, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallExteriorRepLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0905")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 46, frameQualityPercent: 30, shellQualityPercent: 52, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallExteriorRepShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0906")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 50, frameQualityPercent: 33, shellQualityPercent: 50, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallRedVentsDoor1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0907")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 53, frameQualityPercent: 25, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallRedVentsDoor2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0908")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 45, frameQualityPercent: 30, shellQualityPercent: 55, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallRedVentsLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0909")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 45, frameQualityPercent: 25, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallRedVentsShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0910")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 54, frameQualityPercent: 29, shellQualityPercent: 54, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallRepublicIntArch, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0911")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 48, frameQualityPercent: 33, shellQualityPercent: 52, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallRepublicIntDoor, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0912")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 46, frameQualityPercent: 28, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallRepublicIntLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0913")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 46, frameQualityPercent: 29, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallRepublicIntShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0914")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 51, frameQualityPercent: 30, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallTanPanelDoor1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0915")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 51, frameQualityPercent: 25, shellQualityPercent: 54, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallTanPanelDoor2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0916")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 45, frameQualityPercent: 25, shellQualityPercent: 56, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallTanPanelLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0917")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 45, frameQualityPercent: 33, shellQualityPercent: 52, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallTanPanelShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0918")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 49, frameQualityPercent: 28, shellQualityPercent: 54, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallTanPanelWindow, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0919")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 47, frameQualityPercent: 29, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallTanVentsDoor1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0920")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 47, frameQualityPercent: 33, shellQualityPercent: 50, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallTanVentsDoor2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0921")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 50, frameQualityPercent: 26, shellQualityPercent: 54, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallTanVentsLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0922")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 47, frameQualityPercent: 27, shellQualityPercent: 52, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallTanVentsShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0923")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 54, frameQualityPercent: 25, shellQualityPercent: 58, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallWhitePanelDoor1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0924")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 45, frameQualityPercent: 25, shellQualityPercent: 53, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallWhitePanelDoor2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0925")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 52, frameQualityPercent: 25, shellQualityPercent: 51, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallWhitePanelLong, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0926")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 47, frameQualityPercent: 27, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallWhitePanelShort, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0927")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 49, frameQualityPercent: 29, shellQualityPercent: 55, wearRate: 6);

        _builder.Create(RecipeType.SwtorWallWhitePanelWindow, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0928")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 46, frameQualityPercent: 26, shellQualityPercent: 54, wearRate: 6);

        _builder.Create(RecipeType.SwtorWall001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0929")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 45, frameQualityPercent: 27, shellQualityPercent: 54, wearRate: 5);

        _builder.Create(RecipeType.SwtorWall002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0930")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 46, frameQualityPercent: 31, shellQualityPercent: 53, wearRate: 4);

        _builder.Create(RecipeType.SwtorWall003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Wall)
            .Resref("structure_0931")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 46, frameQualityPercent: 26, shellQualityPercent: 56, wearRate: 5);
    }

    private void Electronics()
    {
        _builder.Create(RecipeType.SwtorAuctionHouse001On, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0455")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 23, frameQualityPercent: 24, shellQualityPercent: 30, wearRate: 6);

        _builder.Create(RecipeType.SwtorAuctionHouse002Off, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0456")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 20, frameQualityPercent: 21, shellQualityPercent: 26, wearRate: 6);

        _builder.Create(RecipeType.SwtorBiochemicalStation, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0486")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 24, frameQualityPercent: 18, shellQualityPercent: 30, wearRate: 5);

        _builder.Create(RecipeType.SwtorComputer1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0515")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 30, frameQualityPercent: 20, shellQualityPercent: 25, wearRate: 6);

        _builder.Create(RecipeType.SwtorComputer2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0516")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 22, frameQualityPercent: 16, shellQualityPercent: 24, wearRate: 5);

        _builder.Create(RecipeType.SwtorComputer3, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0517")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 27, frameQualityPercent: 21, shellQualityPercent: 26, wearRate: 5);

        _builder.Create(RecipeType.SwtorComputer4, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0518")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 25, frameQualityPercent: 17, shellQualityPercent: 27, wearRate: 6);

        _builder.Create(RecipeType.SwtorComputer6, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0519")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 28, frameQualityPercent: 22, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorComputer7, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0520")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 21, frameQualityPercent: 22, shellQualityPercent: 27, wearRate: 7);

        _builder.Create(RecipeType.SwtorComputer8, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0521")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 24, frameQualityPercent: 16, shellQualityPercent: 20, wearRate: 6);

        _builder.Create(RecipeType.SwtorConveyorScreenOff, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0542")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("elec_good", 4)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 29, frameQualityPercent: 20, shellQualityPercent: 24, wearRate: 7);

        _builder.Create(RecipeType.SwtorConveyorScreenOn, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0543")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 2)
            .Component("elec_good", 4)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 28, frameQualityPercent: 19, shellQualityPercent: 20, wearRate: 5);

        _builder.Create(RecipeType.SwtorDatapad, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0557")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 6)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 25, frameQualityPercent: 17, shellQualityPercent: 21, wearRate: 5);

        _builder.Create(RecipeType.SwtorDroidAssemblyTerminal, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0565")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 25, frameQualityPercent: 16, shellQualityPercent: 28, wearRate: 5);

        _builder.Create(RecipeType.SwtorFabricationTerminal, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0571")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 30, frameQualityPercent: 19, shellQualityPercent: 28, wearRate: 5);

        _builder.Create(RecipeType.SwtorGeneratorLights, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0624")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 25, frameQualityPercent: 18, shellQualityPercent: 23, wearRate: 6);

        _builder.Create(RecipeType.SwtorGeneratorOff, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0625")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 27, frameQualityPercent: 25, shellQualityPercent: 24, wearRate: 6);

        _builder.Create(RecipeType.SwtorGeneratorWall, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0626")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 26, frameQualityPercent: 20, shellQualityPercent: 20, wearRate: 5);

        _builder.Create(RecipeType.SwtorGenerator003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0627")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 26, frameQualityPercent: 16, shellQualityPercent: 21, wearRate: 6);

        _builder.Create(RecipeType.SwtorGenerator004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0628")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 29, frameQualityPercent: 20, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorGenerator05, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0629")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 23, frameQualityPercent: 24, shellQualityPercent: 23, wearRate: 5);

        _builder.Create(RecipeType.SwtorGenerator06, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0630")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 24, frameQualityPercent: 22, shellQualityPercent: 30, wearRate: 5);

        _builder.Create(RecipeType.SwtorGeneratorPipes, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0631")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 26, frameQualityPercent: 16, shellQualityPercent: 26, wearRate: 6);

        _builder.Create(RecipeType.SwtorJukebox001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0659")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 23, frameQualityPercent: 23, shellQualityPercent: 21, wearRate: 5);

        _builder.Create(RecipeType.SwtorJukebox002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0660")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 24, frameQualityPercent: 22, shellQualityPercent: 28, wearRate: 6);

        _builder.Create(RecipeType.SwtorJukebox003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0661")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 27, frameQualityPercent: 16, shellQualityPercent: 26, wearRate: 5);

        _builder.Create(RecipeType.SwtorJukebox004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0662")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 23, frameQualityPercent: 24, shellQualityPercent: 21, wearRate: 6);

        _builder.Create(RecipeType.SwtorJukebox005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0663")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 27, frameQualityPercent: 20, shellQualityPercent: 22, wearRate: 4);

        _builder.Create(RecipeType.SwtorKiosk001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0666")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 22, frameQualityPercent: 17, shellQualityPercent: 26, wearRate: 6);

        _builder.Create(RecipeType.SwtorKiosk002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0667")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 30, frameQualityPercent: 23, shellQualityPercent: 30, wearRate: 7);

        _builder.Create(RecipeType.SwtorKiosk003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0668")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 23, frameQualityPercent: 23, shellQualityPercent: 21, wearRate: 6);

        _builder.Create(RecipeType.SwtorKiosk004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0669")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 24, frameQualityPercent: 21, shellQualityPercent: 25, wearRate: 5);

        _builder.Create(RecipeType.SwtorKiosk005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0670")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 21, frameQualityPercent: 25, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorKiosk006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0671")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 22, frameQualityPercent: 20, shellQualityPercent: 25, wearRate: 5);

        _builder.Create(RecipeType.SwtorParkingmeter, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0694")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 29, frameQualityPercent: 19, shellQualityPercent: 27, wearRate: 7);

        _builder.Create(RecipeType.SwtorPhonebooth, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0695")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 23, frameQualityPercent: 16, shellQualityPercent: 25, wearRate: 5);

        _builder.Create(RecipeType.SwtorPipes001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0722")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 22, frameQualityPercent: 25, shellQualityPercent: 26, wearRate: 6);

        _builder.Create(RecipeType.SwtorPipes002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0723")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 22, frameQualityPercent: 20, shellQualityPercent: 20, wearRate: 7);

        _builder.Create(RecipeType.SwtorPowerCorePair, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0763")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 24, frameQualityPercent: 25, shellQualityPercent: 20, wearRate: 7);

        _builder.Create(RecipeType.SwtorPowerCoreStation, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0764")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 27, frameQualityPercent: 21, shellQualityPercent: 23, wearRate: 7);

        _builder.Create(RecipeType.SwtorRadarWallMounted, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0771")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 25, frameQualityPercent: 17, shellQualityPercent: 26, wearRate: 5);

        _builder.Create(RecipeType.SwtorRadar001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0772")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 22, frameQualityPercent: 25, shellQualityPercent: 20, wearRate: 5);

        _builder.Create(RecipeType.SwtorRefinery, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0773")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 25, frameQualityPercent: 22, shellQualityPercent: 30, wearRate: 7);

        _builder.Create(RecipeType.SwtorServer001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0801")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 2)
            .Component("ref_plagionite", 1)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 29, frameQualityPercent: 21, shellQualityPercent: 27, wearRate: 5);

        _builder.Create(RecipeType.SwtorSlotMachine1Light, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0808")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 29, frameQualityPercent: 21, shellQualityPercent: 22, wearRate: 6);

        _builder.Create(RecipeType.SwtorSlotMachine2Light, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0809")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 27, frameQualityPercent: 22, shellQualityPercent: 26, wearRate: 6);

        _builder.Create(RecipeType.SwtorSlotMachine3Light, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0810")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 29, frameQualityPercent: 24, shellQualityPercent: 30, wearRate: 4);

        _builder.Create(RecipeType.SwtorSlotMachine4Light, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0811")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 30, frameQualityPercent: 15, shellQualityPercent: 30, wearRate: 5);

        _builder.Create(RecipeType.SwtorSlotMachine5Dark, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0812")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 21, frameQualityPercent: 19, shellQualityPercent: 24, wearRate: 5);

        _builder.Create(RecipeType.SwtorSolarPanel001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0819")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 23, frameQualityPercent: 16, shellQualityPercent: 26, wearRate: 5);

        _builder.Create(RecipeType.SwtorSolarPanel002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0820")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 20, frameQualityPercent: 21, shellQualityPercent: 24, wearRate: 7);

        _builder.Create(RecipeType.SwtorTablet, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0845")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 6)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 29, frameQualityPercent: 19, shellQualityPercent: 20, wearRate: 6);

        _builder.Create(RecipeType.SwtorTerminalImp01On, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0850")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 26, frameQualityPercent: 15, shellQualityPercent: 30, wearRate: 7);

        _builder.Create(RecipeType.SwtorTerminalImpWall01Off, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0851")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 20, frameQualityPercent: 23, shellQualityPercent: 30, wearRate: 5);

        _builder.Create(RecipeType.SwtorTerminalImpWall01On, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0852")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 28, frameQualityPercent: 19, shellQualityPercent: 23, wearRate: 5);

        _builder.Create(RecipeType.SwtorTerminalMarketKiosk, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0853")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 25, frameQualityPercent: 22, shellQualityPercent: 23, wearRate: 5);

        _builder.Create(RecipeType.SwtorTransmitter1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0861")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 29, frameQualityPercent: 15, shellQualityPercent: 20, wearRate: 6);

        _builder.Create(RecipeType.SwtorTurret001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0864")
            .Level(40)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("elec_imperfect", 5)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 25, frameQualityPercent: 15, shellQualityPercent: 23, wearRate: 7);

        _builder.Create(RecipeType.SwtorTurret002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0865")
            .Level(40)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("elec_imperfect", 5)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 24, frameQualityPercent: 24, shellQualityPercent: 20, wearRate: 6);

        _builder.Create(RecipeType.SwtorTurret003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0866")
            .Level(40)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("elec_imperfect", 5)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 20, frameQualityPercent: 17, shellQualityPercent: 27, wearRate: 8);

        _builder.Create(RecipeType.SwtorTurret004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0867")
            .Level(40)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("elec_imperfect", 5)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 25, frameQualityPercent: 15, shellQualityPercent: 27, wearRate: 8);

        _builder.Create(RecipeType.SwtorTurret005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0868")
            .Level(40)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("elec_imperfect", 5)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 27, frameQualityPercent: 17, shellQualityPercent: 20, wearRate: 8);

        _builder.Create(RecipeType.SwtorUpgradeTerminal002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0869")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 26, frameQualityPercent: 21, shellQualityPercent: 20, wearRate: 5);

        _builder.Create(RecipeType.SwtorVendingMachine01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0878")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 30, frameQualityPercent: 15, shellQualityPercent: 27, wearRate: 6);

        _builder.Create(RecipeType.SwtorVendorBank, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0883")
            .Level(24)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 3)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 21, frameQualityPercent: 17, shellQualityPercent: 28, wearRate: 5);
    }

    private void Fixtures()
    {
        _builder.Create(RecipeType.SwtorBanner001Czerka, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0461")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 21, frameQualityPercent: 27, shellQualityPercent: 26, wearRate: 4);

        _builder.Create(RecipeType.SwtorBanner002Czerka, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0462")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 27, frameQualityPercent: 34, shellQualityPercent: 23, wearRate: 3);

        _builder.Create(RecipeType.SwtorBanner003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0463")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 24, frameQualityPercent: 30, shellQualityPercent: 27, wearRate: 4);

        _builder.Create(RecipeType.SwtorBanner004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0464")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 28, frameQualityPercent: 25, shellQualityPercent: 24, wearRate: 4);

        _builder.Create(RecipeType.SwtorBiomatterStorage, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0487")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 2)
            .Component("aracia_wood", 1)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 28, frameQualityPercent: 28, shellQualityPercent: 23, wearRate: 8);

        _builder.Create(RecipeType.SwtorBountyBoard001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0490")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 29, frameQualityPercent: 34, shellQualityPercent: 21, wearRate: 5);

        _builder.Create(RecipeType.SwtorCargoContainerYellow, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0499")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 22, frameQualityPercent: 33, shellQualityPercent: 23, wearRate: 7);

        _builder.Create(RecipeType.SwtorClothesline01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0512")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 30, frameQualityPercent: 31, shellQualityPercent: 28, wearRate: 3);

        _builder.Create(RecipeType.SwtorClothesline02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0513")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 27, wearRate: 5);

        _builder.Create(RecipeType.SwtorCompactor001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0514")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 28, frameQualityPercent: 25, shellQualityPercent: 28, wearRate: 9);

        _builder.Create(RecipeType.SwtorConstructionDumpster001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0522")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 23, frameQualityPercent: 30, shellQualityPercent: 22, wearRate: 9);

        _builder.Create(RecipeType.SwtorConstructionDumpster002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0523")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 20, frameQualityPercent: 33, shellQualityPercent: 24, wearRate: 8);

        _builder.Create(RecipeType.SwtorConstructionDumpster003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0524")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 26, frameQualityPercent: 35, shellQualityPercent: 24, wearRate: 7);

        _builder.Create(RecipeType.SwtorConstructionDumpster004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0525")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 25, frameQualityPercent: 25, shellQualityPercent: 30, wearRate: 9);

        _builder.Create(RecipeType.SwtorConstructionDumpster005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0526")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 25, frameQualityPercent: 25, shellQualityPercent: 28, wearRate: 7);

        _builder.Create(RecipeType.SwtorConstructionDumpster006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0527")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 28, frameQualityPercent: 29, shellQualityPercent: 28, wearRate: 7);

        _builder.Create(RecipeType.SwtorConstructionDumpster007, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0528")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 28, frameQualityPercent: 26, shellQualityPercent: 25, wearRate: 9);

        _builder.Create(RecipeType.SwtorConstructionDumpster008, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0529")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 25, wearRate: 7);

        _builder.Create(RecipeType.SwtorControlTower001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0540")
            .Level(40)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("elec_imperfect", 5)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 25, frameQualityPercent: 29, shellQualityPercent: 28, wearRate: 7);

        _builder.Create(RecipeType.SwtorConveyor, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0541")
            .Level(37)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("aracia_wood", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 30, frameQualityPercent: 32, shellQualityPercent: 30, wearRate: 8);

        _builder.Create(RecipeType.SwtorCrane001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0545")
            .Level(40)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("elec_imperfect", 5)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 21, frameQualityPercent: 25, shellQualityPercent: 29, wearRate: 6);

        _builder.Create(RecipeType.SwtorCzerkaSign01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0555")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 27, frameQualityPercent: 33, shellQualityPercent: 28, wearRate: 5);

        _builder.Create(RecipeType.SwtorCzerkaSign02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0556")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 24, frameQualityPercent: 26, shellQualityPercent: 28, wearRate: 4);

        _builder.Create(RecipeType.SwtorDumpsterEmpty, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0566")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 22, frameQualityPercent: 29, shellQualityPercent: 20, wearRate: 7);

        _builder.Create(RecipeType.SwtorDumpster001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0567")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 22, frameQualityPercent: 26, shellQualityPercent: 26, wearRate: 9);

        _builder.Create(RecipeType.SwtorDumpster002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0568")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 27, frameQualityPercent: 25, shellQualityPercent: 22, wearRate: 8);

        _builder.Create(RecipeType.SwtorElevator, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0569")
            .Level(37)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("aracia_wood", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 26, wearRate: 7);

        _builder.Create(RecipeType.SwtorElevator002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0570")
            .Level(37)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("aracia_wood", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 20, frameQualityPercent: 32, shellQualityPercent: 29, wearRate: 8);

        _builder.Create(RecipeType.SwtorForklift001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0623")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 22, frameQualityPercent: 26, shellQualityPercent: 23, wearRate: 9);

        _builder.Create(RecipeType.SwtorGSISign, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0634")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 24, frameQualityPercent: 28, shellQualityPercent: 25, wearRate: 4);

        _builder.Create(RecipeType.SwtorHoloCantinaSign, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0636")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 25, frameQualityPercent: 25, shellQualityPercent: 29, wearRate: 8);

        _builder.Create(RecipeType.SwtorHologram001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0637")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 23, frameQualityPercent: 25, shellQualityPercent: 24, wearRate: 8);

        _builder.Create(RecipeType.SwtorHologram002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0638")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 24, frameQualityPercent: 35, shellQualityPercent: 29, wearRate: 8);

        _builder.Create(RecipeType.SwtorHologram003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0639")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 30, frameQualityPercent: 31, shellQualityPercent: 30, wearRate: 8);

        _builder.Create(RecipeType.SwtorHologram004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0640")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 24, frameQualityPercent: 30, shellQualityPercent: 24, wearRate: 9);

        _builder.Create(RecipeType.SwtorHologram005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0641")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 25, frameQualityPercent: 27, shellQualityPercent: 25, wearRate: 7);

        _builder.Create(RecipeType.SwtorHologram006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0642")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 27, frameQualityPercent: 32, shellQualityPercent: 25, wearRate: 7);

        _builder.Create(RecipeType.SwtorHologram007, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0643")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 30, frameQualityPercent: 35, shellQualityPercent: 26, wearRate: 8);

        _builder.Create(RecipeType.SwtorHologram008, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0644")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 23, frameQualityPercent: 31, shellQualityPercent: 30, wearRate: 7);

        _builder.Create(RecipeType.SwtorHologram009, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0645")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 29, frameQualityPercent: 29, shellQualityPercent: 25, wearRate: 8);

        _builder.Create(RecipeType.SwtorHologram010, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0646")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 25, frameQualityPercent: 29, shellQualityPercent: 24, wearRate: 9);

        _builder.Create(RecipeType.SwtorHolotree1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0647")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 24, frameQualityPercent: 35, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorHolotree2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0648")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 30, frameQualityPercent: 35, shellQualityPercent: 21, wearRate: 9);

        _builder.Create(RecipeType.SwtorHoverHolo002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0649")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 26, frameQualityPercent: 25, shellQualityPercent: 21, wearRate: 9);

        _builder.Create(RecipeType.SwtorJediLibrary001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0653")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 27, frameQualityPercent: 35, shellQualityPercent: 20, wearRate: 9);

        _builder.Create(RecipeType.SwtorJediLibrary002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0654")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 25, frameQualityPercent: 34, shellQualityPercent: 23, wearRate: 8);

        _builder.Create(RecipeType.SwtorJediLibrary003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0655")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 23, frameQualityPercent: 26, shellQualityPercent: 25, wearRate: 8);

        _builder.Create(RecipeType.SwtorJediLibrary004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0656")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 29, frameQualityPercent: 27, shellQualityPercent: 25, wearRate: 7);

        _builder.Create(RecipeType.SwtorJediLibrary005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0657")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 24, frameQualityPercent: 33, shellQualityPercent: 27, wearRate: 7);

        _builder.Create(RecipeType.SwtorJediLibrary006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0658")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 24, wearRate: 9);

        _builder.Create(RecipeType.SwtorKoltoTank01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0672")
            .Level(47)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 4)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 21, frameQualityPercent: 32, shellQualityPercent: 21, wearRate: 6);

        _builder.Create(RecipeType.SwtorMine, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0691")
            .Level(37)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("aracia_wood", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 29, frameQualityPercent: 25, shellQualityPercent: 21, wearRate: 8);

        _builder.Create(RecipeType.SwtorMiningDrill001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0692")
            .Level(37)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("aracia_wood", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 28, frameQualityPercent: 30, shellQualityPercent: 21, wearRate: 7);

        _builder.Create(RecipeType.SwtorPlantBush01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0724")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 20, frameQualityPercent: 33, shellQualityPercent: 25, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantBush02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0725")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 29, frameQualityPercent: 27, shellQualityPercent: 21, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlantCattials, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0726")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 29, frameQualityPercent: 32, shellQualityPercent: 26, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantCrops01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0727")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 23, frameQualityPercent: 35, shellQualityPercent: 25, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantCrops02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0728")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 21, frameQualityPercent: 34, shellQualityPercent: 25, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantCrops03, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0729")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 26, frameQualityPercent: 25, shellQualityPercent: 30, wearRate: 4);

        _builder.Create(RecipeType.SwtorPlantCrops04, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0730")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 29, frameQualityPercent: 34, shellQualityPercent: 21, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlantFern01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0731")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 26, frameQualityPercent: 29, shellQualityPercent: 25, wearRate: 4);

        _builder.Create(RecipeType.SwtorPlantFern02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0732")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 20, frameQualityPercent: 28, shellQualityPercent: 28, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantFern03, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0733")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 26, frameQualityPercent: 33, shellQualityPercent: 24, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantFernGroup01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0734")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 26, frameQualityPercent: 29, shellQualityPercent: 28, wearRate: 4);

        _builder.Create(RecipeType.SwtorPlantFungusHanging01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0735")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 30, frameQualityPercent: 34, shellQualityPercent: 22, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantFungusHanging02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0736")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 20, frameQualityPercent: 35, shellQualityPercent: 26, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlantGrassDead01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0737")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 21, frameQualityPercent: 32, shellQualityPercent: 22, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantGrassSwamp01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0738")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 26, frameQualityPercent: 25, shellQualityPercent: 25, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlantGrassSwamp02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0739")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 26, frameQualityPercent: 28, shellQualityPercent: 24, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantGrassSwamp03, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0740")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 29, frameQualityPercent: 34, shellQualityPercent: 25, wearRate: 4);

        _builder.Create(RecipeType.SwtorPlantPotpod01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0741")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 20, frameQualityPercent: 35, shellQualityPercent: 23, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlantSilverfern, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0742")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 26, frameQualityPercent: 35, shellQualityPercent: 26, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantStalk01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0743")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 24, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlantStalk02, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0744")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 30, frameQualityPercent: 30, shellQualityPercent: 29, wearRate: 3);

        _builder.Create(RecipeType.SwtorPlantVinepost, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0745")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 24, frameQualityPercent: 29, shellQualityPercent: 23, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlant1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0746")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 28, frameQualityPercent: 29, shellQualityPercent: 28, wearRate: 5);

        _builder.Create(RecipeType.SwtorPlaqueStanding, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0754")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 20, frameQualityPercent: 28, shellQualityPercent: 26, wearRate: 4);

        _builder.Create(RecipeType.SwtorPosterAdventureAwaits, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0755")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 25, frameQualityPercent: 31, shellQualityPercent: 21, wearRate: 4);

        _builder.Create(RecipeType.SwtorPosterConstructionOffLimits, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0756")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 26, frameQualityPercent: 26, shellQualityPercent: 25, wearRate: 3);

        _builder.Create(RecipeType.SwtorPosterDroidRechargeSection, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0757")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 29, frameQualityPercent: 34, shellQualityPercent: 26, wearRate: 5);

        _builder.Create(RecipeType.SwtorPosterMustRegisterWPlanet, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0758")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 30, frameQualityPercent: 25, shellQualityPercent: 29, wearRate: 4);

        _builder.Create(RecipeType.SwtorPosterNeutralStation, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0759")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 26, frameQualityPercent: 30, shellQualityPercent: 23, wearRate: 4);

        _builder.Create(RecipeType.SwtorPosterTowZone, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0760")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 24, frameQualityPercent: 26, shellQualityPercent: 28, wearRate: 5);

        _builder.Create(RecipeType.SwtorPoster1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0761")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 21, frameQualityPercent: 32, shellQualityPercent: 25, wearRate: 3);

        _builder.Create(RecipeType.SwtorPoster2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0762")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 20, frameQualityPercent: 33, shellQualityPercent: 27, wearRate: 4);

        _builder.Create(RecipeType.SwtorProjectionHolo001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0765")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 28, frameQualityPercent: 25, shellQualityPercent: 20, wearRate: 8);

        _builder.Create(RecipeType.SwtorProjectionHolo002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0766")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 20, frameQualityPercent: 27, shellQualityPercent: 30, wearRate: 8);

        _builder.Create(RecipeType.SwtorProjectionHolo003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0767")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 23, frameQualityPercent: 26, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorProjectionHolo004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0768")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 20, frameQualityPercent: 32, shellQualityPercent: 25, wearRate: 9);

        _builder.Create(RecipeType.SwtorProjectionHolo005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0769")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 29, frameQualityPercent: 28, shellQualityPercent: 27, wearRate: 9);

        _builder.Create(RecipeType.SwtorProjectionHolo006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0770")
            .Level(44)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_imperfect", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 27, frameQualityPercent: 32, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorScaffold001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0799")
            .Level(37)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("aracia_wood", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 25, frameQualityPercent: 26, shellQualityPercent: 30, wearRate: 8);

        _builder.Create(RecipeType.SwtorScaffold002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0800")
            .Level(37)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 4)
            .Component("aracia_wood", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 20, frameQualityPercent: 28, shellQualityPercent: 28, wearRate: 7);

        _builder.Create(RecipeType.SwtorSignHangingTech, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0802")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 23, frameQualityPercent: 26, shellQualityPercent: 30, wearRate: 3);

        _builder.Create(RecipeType.SwtorSignStreetCoreWalkway, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0803")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 27, frameQualityPercent: 34, shellQualityPercent: 29, wearRate: 4);

        _builder.Create(RecipeType.SwtorSignStreetDayMoon, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0804")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 23, frameQualityPercent: 31, shellQualityPercent: 25, wearRate: 5);

        _builder.Create(RecipeType.SwtorSignStreetDirection, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0805")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 26, frameQualityPercent: 32, shellQualityPercent: 27, wearRate: 5);

        _builder.Create(RecipeType.SwtorSignStreetWatch, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0806")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 25, frameQualityPercent: 35, shellQualityPercent: 25, wearRate: 4);

        _builder.Create(RecipeType.SwtorSignpostTaxiHolo, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0807")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 25, frameQualityPercent: 28, shellQualityPercent: 24, wearRate: 4);

        _builder.Create(RecipeType.SwtorTankLarge, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0846")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 20, frameQualityPercent: 25, shellQualityPercent: 26, wearRate: 9);

        _builder.Create(RecipeType.SwtorTankSmall, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0847")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 20, frameQualityPercent: 30, shellQualityPercent: 24, wearRate: 9);

        _builder.Create(RecipeType.SwtorVehicle001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0870")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 25, wearRate: 7);

        _builder.Create(RecipeType.SwtorVehicle002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0871")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 27, frameQualityPercent: 26, shellQualityPercent: 25, wearRate: 7);

        _builder.Create(RecipeType.SwtorVehicle003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0872")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 24, frameQualityPercent: 31, shellQualityPercent: 29, wearRate: 7);

        _builder.Create(RecipeType.SwtorVehicle004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0873")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 24, wearRate: 8);

        _builder.Create(RecipeType.SwtorVehicle005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0874")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 26, frameQualityPercent: 26, shellQualityPercent: 20, wearRate: 9);

        _builder.Create(RecipeType.SwtorVehicle006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0875")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 22, frameQualityPercent: 32, shellQualityPercent: 28, wearRate: 8);

        _builder.Create(RecipeType.SwtorVehicle007, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0876")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 22, frameQualityPercent: 34, shellQualityPercent: 20, wearRate: 8);

        _builder.Create(RecipeType.SwtorVehicle008, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0877")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 26, frameQualityPercent: 28, shellQualityPercent: 29, wearRate: 8);

        _builder.Create(RecipeType.SwtorVendorSign1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0885")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 24, frameQualityPercent: 35, shellQualityPercent: 30, wearRate: 5);

        _builder.Create(RecipeType.SwtorVendorSign2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0886")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 28, frameQualityPercent: 28, shellQualityPercent: 21, wearRate: 3);

        _builder.Create(RecipeType.SwtorVendorSign3, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0887")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 30, frameQualityPercent: 33, shellQualityPercent: 21, wearRate: 5);

        _builder.Create(RecipeType.SwtorVendorSign4, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0888")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 30, frameQualityPercent: 31, shellQualityPercent: 23, wearRate: 3);

        _builder.Create(RecipeType.SwtorVendorSign5, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0889")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 30, frameQualityPercent: 32, shellQualityPercent: 25, wearRate: 4);

        _builder.Create(RecipeType.SwtorWallDecor001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0932")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 30, frameQualityPercent: 33, shellQualityPercent: 24, wearRate: 5);

        _builder.Create(RecipeType.SwtorWallDecor002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0933")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 30, frameQualityPercent: 35, shellQualityPercent: 22, wearRate: 4);

        _builder.Create(RecipeType.SwtorWardrobe2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0934")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 2)
            .Component("fiberp_imperfect", 2)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 23, frameQualityPercent: 28, shellQualityPercent: 25, wearRate: 8);

        _builder.Create(RecipeType.SwtorWardrobe3, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0935")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 2)
            .Component("fiberp_imperfect", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 23, frameQualityPercent: 26, shellQualityPercent: 29, wearRate: 6);

        _builder.Create(RecipeType.SwtorWardrobe4, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0936")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 2)
            .Component("fiberp_imperfect", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 29, frameQualityPercent: 33, shellQualityPercent: 26, wearRate: 8);

        _builder.Create(RecipeType.SwtorWaterTower, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0937")
            .Level(46)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 3)
            .Component("elec_high", 4)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 26, frameQualityPercent: 31, shellQualityPercent: 27, wearRate: 9);
    }

    private void Surfaces()
    {
        _builder.Create(RecipeType.SwtorBar001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0465")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 43, frameQualityPercent: 37, shellQualityPercent: 44, wearRate: 6);

        _builder.Create(RecipeType.SwtorBar002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0466")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 47, frameQualityPercent: 38, shellQualityPercent: 43, wearRate: 5);

        _builder.Create(RecipeType.SwtorBar003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0467")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 48, frameQualityPercent: 36, shellQualityPercent: 41, wearRate: 5);

        _builder.Create(RecipeType.SwtorBar004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0468")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 43, frameQualityPercent: 38, shellQualityPercent: 44, wearRate: 6);

        _builder.Create(RecipeType.SwtorBar005, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0469")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 46, frameQualityPercent: 43, shellQualityPercent: 40, wearRate: 5);

        _builder.Create(RecipeType.SwtorBar0052, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0470")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 41, frameQualityPercent: 37, shellQualityPercent: 43, wearRate: 6);

        _builder.Create(RecipeType.SwtorBar006, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0471")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 47, frameQualityPercent: 41, shellQualityPercent: 42, wearRate: 5);

        _builder.Create(RecipeType.SwtorBar007, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0472")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 41, frameQualityPercent: 35, shellQualityPercent: 47, wearRate: 6);

        _builder.Create(RecipeType.SwtorBar008, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0473")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 46, frameQualityPercent: 41, shellQualityPercent: 40, wearRate: 7);

        _builder.Create(RecipeType.SwtorBar009, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0474")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 48, frameQualityPercent: 39, shellQualityPercent: 42, wearRate: 5);

        _builder.Create(RecipeType.SwtorBar010, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0475")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 41, frameQualityPercent: 42, shellQualityPercent: 42, wearRate: 5);

        _builder.Create(RecipeType.SwtorBar011, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0476")
            .Level(25)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 5)
            .Component("fiberp_good", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 44, frameQualityPercent: 40, shellQualityPercent: 43, wearRate: 5);

        _builder.Create(RecipeType.SwtorMarketStand01Empty, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0681")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 43, frameQualityPercent: 40, shellQualityPercent: 44, wearRate: 7);

        _builder.Create(RecipeType.SwtorMarketStand02Full, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0682")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 49, frameQualityPercent: 35, shellQualityPercent: 45, wearRate: 7);

        _builder.Create(RecipeType.SwtorMarketStand03Full, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0683")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 47, frameQualityPercent: 38, shellQualityPercent: 43, wearRate: 5);

        _builder.Create(RecipeType.SwtorMarketStand04Empty, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0684")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 47, frameQualityPercent: 37, shellQualityPercent: 48, wearRate: 7);

        _builder.Create(RecipeType.SwtorMarketStand05Full, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0685")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 41, frameQualityPercent: 38, shellQualityPercent: 45, wearRate: 6);

        _builder.Create(RecipeType.SwtorMarketStand06Empty, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0686")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 50, frameQualityPercent: 36, shellQualityPercent: 46, wearRate: 5);

        _builder.Create(RecipeType.SwtorMarketStand07Full, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0687")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 48, frameQualityPercent: 39, shellQualityPercent: 43, wearRate: 7);

        _builder.Create(RecipeType.SwtorMarketStand08Empty, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0688")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 40, frameQualityPercent: 36, shellQualityPercent: 49, wearRate: 5);

        _builder.Create(RecipeType.SwtorMarketStand09Full, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0689")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 46, frameQualityPercent: 42, shellQualityPercent: 40, wearRate: 5);

        _builder.Create(RecipeType.SwtorMarketStand10Full, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0690")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 46, frameQualityPercent: 35, shellQualityPercent: 42, wearRate: 5);

        _builder.Create(RecipeType.SwtorRepairDesk, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0774")
            .Level(27)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 4)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 43, frameQualityPercent: 36, shellQualityPercent: 46, wearRate: 7);

        _builder.Create(RecipeType.SwtorSmithingBench, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0814")
            .Level(27)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 4)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 42, frameQualityPercent: 37, shellQualityPercent: 43, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableCantinaLarge, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0826")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 49, frameQualityPercent: 41, shellQualityPercent: 43, wearRate: 8);

        _builder.Create(RecipeType.SwtorTableCantinaLargeFull, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0827")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 44, shellSizePercent: 41, frameQualityPercent: 36, shellQualityPercent: 44, wearRate: 6);

        _builder.Create(RecipeType.SwtorTableCantinaSmall, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0828")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 46, frameQualityPercent: 38, shellQualityPercent: 44, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableCantinaSmallFull, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0829")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 44, frameQualityPercent: 43, shellQualityPercent: 41, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableConferenceBlack, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0830")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 45, frameQualityPercent: 35, shellQualityPercent: 45, wearRate: 8);

        _builder.Create(RecipeType.SwtorTableConferenceBrown, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0831")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 41, frameQualityPercent: 38, shellQualityPercent: 44, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableConferenceGrey, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0832")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 49, frameQualityPercent: 41, shellQualityPercent: 42, wearRate: 6);

        _builder.Create(RecipeType.SwtorTableImp01Off, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0833")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 44, shellSizePercent: 40, frameQualityPercent: 40, shellQualityPercent: 43, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableImp01On, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0834")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 41, frameQualityPercent: 40, shellQualityPercent: 45, wearRate: 6);

        _builder.Create(RecipeType.SwtorTableImpDesk01Off, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0835")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 46, frameQualityPercent: 36, shellQualityPercent: 49, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableImpDesk01On, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0836")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 40, frameQualityPercent: 35, shellQualityPercent: 42, wearRate: 6);

        _builder.Create(RecipeType.SwtorTable1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0837")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 48, frameQualityPercent: 35, shellQualityPercent: 41, wearRate: 6);

        _builder.Create(RecipeType.SwtorTable2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0838")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 42, frameQualityPercent: 36, shellQualityPercent: 47, wearRate: 7);

        _builder.Create(RecipeType.SwtorTable3, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0839")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 46, frameQualityPercent: 37, shellQualityPercent: 43, wearRate: 8);

        _builder.Create(RecipeType.SwtorTable5, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0840")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 40, frameQualityPercent: 41, shellQualityPercent: 40, wearRate: 6);

        _builder.Create(RecipeType.SwtorTable6, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0841")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 43, frameQualityPercent: 38, shellQualityPercent: 42, wearRate: 6);

        _builder.Create(RecipeType.SwtorTable7, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0842")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 42, shellSizePercent: 40, frameQualityPercent: 44, shellQualityPercent: 41, wearRate: 6);

        _builder.Create(RecipeType.SwtorTable8, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0843")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 42, frameQualityPercent: 40, shellQualityPercent: 43, wearRate: 6);

        _builder.Create(RecipeType.SwtorTable9, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0844")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 42, frameQualityPercent: 38, shellQualityPercent: 44, wearRate: 8);

        _builder.Create(RecipeType.SwtorVendorArmor, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0879")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 44, frameQualityPercent: 35, shellQualityPercent: 43, wearRate: 7);

        _builder.Create(RecipeType.SwtorVendorGeneral1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0880")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 41, frameQualityPercent: 39, shellQualityPercent: 42, wearRate: 7);

        _builder.Create(RecipeType.SwtorVendorGeneral2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0881")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 42, frameQualityPercent: 38, shellQualityPercent: 43, wearRate: 7);

        _builder.Create(RecipeType.SwtorVendorWeapons, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0882")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 42, frameQualityPercent: 36, shellQualityPercent: 40, wearRate: 7);

        _builder.Create(RecipeType.SwtorVendorTable, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0890")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 43, frameQualityPercent: 36, shellQualityPercent: 44, wearRate: 8);

        _builder.Create(RecipeType.SwtorWorkbench, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0941")
            .Level(27)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 4)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 45, frameQualityPercent: 35, shellQualityPercent: 47, wearRate: 5);

        _builder.Create(RecipeType.SwtorWorkbench002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0942")
            .Level(27)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 4)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 42, frameQualityPercent: 43, shellQualityPercent: 41, wearRate: 6);

        _builder.Create(RecipeType.SwtorWorkbench003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0943")
            .Level(27)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 4)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 45, frameQualityPercent: 39, shellQualityPercent: 40, wearRate: 7);

        _builder.Create(RecipeType.SwtorWorkbench004, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0944")
            .Level(27)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("elec_good", 4)
            .Component("ref_plagionite", 2)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 47, frameQualityPercent: 40, shellQualityPercent: 43, wearRate: 7);
    }

    private void Seating()
    {
        _builder.Create(RecipeType.SwtorBench001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0483")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("aracia_wood", 2)
            .Component("ref_keromber", 1)
            .FabricatorParameters(frameSizePercent: 46, shellSizePercent: 35, frameQualityPercent: 49, shellQualityPercent: 36, wearRate: 6);

        _builder.Create(RecipeType.SwtorBench002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0484")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("aracia_wood", 2)
            .Component("ref_keromber", 1)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 41, frameQualityPercent: 40, shellQualityPercent: 38, wearRate: 7);

        _builder.Create(RecipeType.SwtorBench003, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0485")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("aracia_wood", 2)
            .Component("ref_keromber", 1)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 35, frameQualityPercent: 45, shellQualityPercent: 39, wearRate: 6);

        _builder.Create(RecipeType.SwtorChairCockpitBlack, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0505")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 44, shellSizePercent: 37, frameQualityPercent: 44, shellQualityPercent: 38, wearRate: 3);

        _builder.Create(RecipeType.SwtorChairLeatherRound, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0506")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 39, frameQualityPercent: 41, shellQualityPercent: 43, wearRate: 5);

        _builder.Create(RecipeType.SwtorChairLeatherSimple, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0507")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 42, shellSizePercent: 39, frameQualityPercent: 43, shellQualityPercent: 38, wearRate: 3);

        _builder.Create(RecipeType.SwtorChairPlasteelRepublic, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0508")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 44, frameQualityPercent: 48, shellQualityPercent: 35, wearRate: 5);

        _builder.Create(RecipeType.SwtorChairPlasteelTan, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0509")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 38, frameQualityPercent: 49, shellQualityPercent: 36, wearRate: 5);

        _builder.Create(RecipeType.SwtorChair001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0510")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 48, shellSizePercent: 35, frameQualityPercent: 42, shellQualityPercent: 41, wearRate: 4);

        _builder.Create(RecipeType.SwtorChair002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0511")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 44, shellSizePercent: 35, frameQualityPercent: 40, shellQualityPercent: 45, wearRate: 3);

        _builder.Create(RecipeType.SwtorCouch01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0544")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 41, frameQualityPercent: 43, shellQualityPercent: 37, wearRate: 4);

        _builder.Create(RecipeType.SwtorHuttThrone, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0651")
            .Level(45)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("hyphae_wood", 3)
            .Component("ref_jasioclase", 2)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 41, frameQualityPercent: 40, shellQualityPercent: 35, wearRate: 9);

        _builder.Create(RecipeType.SwtorSofa, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0816")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 37, frameQualityPercent: 42, shellQualityPercent: 38, wearRate: 5);

        _builder.Create(RecipeType.SwtorSofa2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0817")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 46, shellSizePercent: 36, frameQualityPercent: 47, shellQualityPercent: 36, wearRate: 5);

        _builder.Create(RecipeType.SwtorSofa3, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0818")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 36, frameQualityPercent: 42, shellQualityPercent: 43, wearRate: 5);

        _builder.Create(RecipeType.SwtorTortureChair, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0856")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 42, shellSizePercent: 42, frameQualityPercent: 40, shellQualityPercent: 38, wearRate: 4);
    }

    private void Statues()
    {
        _builder.Create(RecipeType.SwtorCargoPallet, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0500")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 58, frameQualityPercent: 22, shellQualityPercent: 59, wearRate: 4);

        _builder.Create(RecipeType.SwtorCrystal001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0552")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 1)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 52, frameQualityPercent: 20, shellQualityPercent: 57, wearRate: 5);

        _builder.Create(RecipeType.SwtorCrystal002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0553")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 50, frameQualityPercent: 23, shellQualityPercent: 60, wearRate: 4);

        _builder.Create(RecipeType.SwtorDebrisPile001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0558")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 50, frameQualityPercent: 24, shellQualityPercent: 58, wearRate: 5);

        _builder.Create(RecipeType.SwtorDebrisPile002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0559")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 55, frameQualityPercent: 23, shellQualityPercent: 59, wearRate: 4);

        _builder.Create(RecipeType.SwtorHuttStatue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0650")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fine_wood", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 52, frameQualityPercent: 22, shellQualityPercent: 55, wearRate: 4);

        _builder.Create(RecipeType.SwtorJunkPile1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0664")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 52, frameQualityPercent: 24, shellQualityPercent: 55, wearRate: 3);

        _builder.Create(RecipeType.SwtorJunkPile2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0665")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 53, frameQualityPercent: 20, shellQualityPercent: 57, wearRate: 4);

        _builder.Create(RecipeType.SwtorPallet001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0693")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 55, frameQualityPercent: 27, shellQualityPercent: 58, wearRate: 5);

        _builder.Create(RecipeType.SwtorSnowPile01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0815")
            .Level(7)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 4)
            .Component("ref_veldite", 2)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 51, frameQualityPercent: 22, shellQualityPercent: 63, wearRate: 3);

        _builder.Create(RecipeType.SwtorStatueVelesFountain, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0821")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fine_wood", 2)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 53, frameQualityPercent: 20, shellQualityPercent: 55, wearRate: 5);

        _builder.Create(RecipeType.SwtorStatue001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0822")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fine_wood", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 50, frameQualityPercent: 20, shellQualityPercent: 62, wearRate: 5);

        _builder.Create(RecipeType.SwtorStatue002, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0823")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fine_wood", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 54, frameQualityPercent: 26, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorTrainingDummy, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0858")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fine_wood", 2)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 56, frameQualityPercent: 22, shellQualityPercent: 62, wearRate: 6);

        _builder.Create(RecipeType.SwtorTrainingDummySide, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0859")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fine_wood", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 50, frameQualityPercent: 26, shellQualityPercent: 57, wearRate: 6);

        _builder.Create(RecipeType.SwtorTrainingDummyImp, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0860")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fine_wood", 2)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 54, frameQualityPercent: 27, shellQualityPercent: 58, wearRate: 5);

        _builder.Create(RecipeType.SwtorWaterfall001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Statues)
            .Resref("structure_0938")
            .Level(27)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 4)
            .Component("fiberp_good", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 50, frameQualityPercent: 23, shellQualityPercent: 62, wearRate: 6);
    }

    private void Flooring()
    {
        _builder.Create(RecipeType.SwtorDecalPaperFloor, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0560")
            .Level(4)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 3)
            .Component("fiberp_ruined", 1)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 57, frameQualityPercent: 23, shellQualityPercent: 61, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorMetalHex, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0591")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 60, frameQualityPercent: 22, shellQualityPercent: 63, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorMetal, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0592")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 61, frameQualityPercent: 21, shellQualityPercent: 61, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorRoadEnd, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0593")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 56, frameQualityPercent: 24, shellQualityPercent: 61, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorRoadT, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0594")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 58, frameQualityPercent: 20, shellQualityPercent: 61, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorRoadTurn, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0595")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 61, frameQualityPercent: 25, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorRoadX, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0596")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 58, frameQualityPercent: 26, shellQualityPercent: 56, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorRoad, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0597")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 57, frameQualityPercent: 20, shellQualityPercent: 58, wearRate: 6);

        _builder.Create(RecipeType.SwtorFloorCrossPanelGrey1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0598")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 55, frameQualityPercent: 24, shellQualityPercent: 57, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorCrossPanelGrey2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0599")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 62, frameQualityPercent: 20, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorDuracreteSlab1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0600")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 62, frameQualityPercent: 24, shellQualityPercent: 61, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorDuracreteSlab2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0601")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 57, frameQualityPercent: 20, shellQualityPercent: 57, wearRate: 6);

        _builder.Create(RecipeType.SwtorFloorDuracreteTile1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0602")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 56, frameQualityPercent: 22, shellQualityPercent: 63, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorDuracreteTile2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0603")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 58, frameQualityPercent: 21, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorDurasteelPanelBlack1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0604")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 57, frameQualityPercent: 21, shellQualityPercent: 64, wearRate: 6);

        _builder.Create(RecipeType.SwtorFloorDurasteelPanelBlack2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0605")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 56, frameQualityPercent: 27, shellQualityPercent: 58, wearRate: 6);

        _builder.Create(RecipeType.SwtorFloorDurasteelPanelGrey1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0606")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 56, frameQualityPercent: 22, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorDurasteelPanelGrey2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0607")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 61, frameQualityPercent: 20, shellQualityPercent: 55, wearRate: 6);

        _builder.Create(RecipeType.SwtorFloorDurasteelPanelTan1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0608")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 62, frameQualityPercent: 23, shellQualityPercent: 60, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorDurasteelPanelTan2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0609")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 59, frameQualityPercent: 23, shellQualityPercent: 58, wearRate: 6);

        _builder.Create(RecipeType.SwtorFloorIndustrialYellow, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0610")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 59, frameQualityPercent: 27, shellQualityPercent: 58, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorMedicalTile1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0611")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 62, frameQualityPercent: 20, shellQualityPercent: 57, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorMedicalTile2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0612")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 57, frameQualityPercent: 28, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorStoneCarved1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0613")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 55, frameQualityPercent: 22, shellQualityPercent: 58, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorStoneCarved2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0614")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 56, frameQualityPercent: 23, shellQualityPercent: 55, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorStonePattern11x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0615")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 62, frameQualityPercent: 20, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorStonePattern12x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0616")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 55, frameQualityPercent: 25, shellQualityPercent: 55, wearRate: 5);

        _builder.Create(RecipeType.SwtorFloorStonePattern21x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0617")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 56, frameQualityPercent: 21, shellQualityPercent: 57, wearRate: 6);

        _builder.Create(RecipeType.SwtorFloorStonePattern22x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0618")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 55, frameQualityPercent: 26, shellQualityPercent: 57, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorStoneTiles1x1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0619")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 55, frameQualityPercent: 20, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorFloorStoneTiles2x2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0620")
            .Level(17)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 4)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 56, frameQualityPercent: 21, shellQualityPercent: 63, wearRate: 6);

        _builder.Create(RecipeType.SwtorGrate001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0632")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 57, frameQualityPercent: 28, shellQualityPercent: 56, wearRate: 6);

        _builder.Create(RecipeType.SwtorRoad13Way, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0775")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 59, frameQualityPercent: 27, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorRoad14Way, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0776")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 59, frameQualityPercent: 24, shellQualityPercent: 57, wearRate: 6);

        _builder.Create(RecipeType.SwtorRoad1Curve, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0777")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 64, frameQualityPercent: 23, shellQualityPercent: 61, wearRate: 5);

        _builder.Create(RecipeType.SwtorRoad1Slab, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0778")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 57, frameQualityPercent: 24, shellQualityPercent: 56, wearRate: 6);

        _builder.Create(RecipeType.SwtorRoad1Straight, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0779")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 3)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 58, frameQualityPercent: 20, shellQualityPercent: 60, wearRate: 6);

        _builder.Create(RecipeType.SwtorRugBlueGeometry, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0780")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 58, frameQualityPercent: 22, shellQualityPercent: 63, wearRate: 5);

        _builder.Create(RecipeType.SwtorRugBlueGeometrySmall, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0781")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 64, frameQualityPercent: 20, shellQualityPercent: 60, wearRate: 5);

        _builder.Create(RecipeType.SwtorRugBlueOctagon, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0782")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 56, frameQualityPercent: 26, shellQualityPercent: 56, wearRate: 3);

        _builder.Create(RecipeType.SwtorRugCreamPattern, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0783")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 64, frameQualityPercent: 28, shellQualityPercent: 55, wearRate: 5);

        _builder.Create(RecipeType.SwtorRugGoldPattern, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0784")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 55, frameQualityPercent: 25, shellQualityPercent: 55, wearRate: 4);

        _builder.Create(RecipeType.SwtorRugGreenOctagon, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0785")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 64, frameQualityPercent: 25, shellQualityPercent: 58, wearRate: 4);

        _builder.Create(RecipeType.SwtorRugJediBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0786")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 55, frameQualityPercent: 20, shellQualityPercent: 57, wearRate: 3);

        _builder.Create(RecipeType.SwtorRugRedOrangePattern, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0787")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 56, frameQualityPercent: 24, shellQualityPercent: 56, wearRate: 4);

        _builder.Create(RecipeType.SwtorRugRoyalDarkBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0788")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 55, frameQualityPercent: 24, shellQualityPercent: 57, wearRate: 4);

        _builder.Create(RecipeType.SwtorRug1, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0789")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 56, frameQualityPercent: 26, shellQualityPercent: 59, wearRate: 3);

        _builder.Create(RecipeType.SwtorRug2, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0790")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 59, frameQualityPercent: 28, shellQualityPercent: 55, wearRate: 3);

        _builder.Create(RecipeType.SwtorRug3, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0791")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 59, frameQualityPercent: 20, shellQualityPercent: 64, wearRate: 5);

        _builder.Create(RecipeType.SwtorRug4, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0792")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 65, frameQualityPercent: 26, shellQualityPercent: 59, wearRate: 5);

        _builder.Create(RecipeType.SwtorRug5, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0793")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 64, frameQualityPercent: 21, shellQualityPercent: 57, wearRate: 5);
    }

    private void Doors()
    {
        _builder.Create(RecipeType.SwtorDoorCoveredEntrance, SkillType.Fabrication)
            .Category(RecipeCategoryType.Doors)
            .Resref("structure_0561")
            .Level(9)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_veldite", 5)
            .Component("wood", 3)
            .FabricatorParameters(frameSizePercent: 45, shellSizePercent: 30, frameQualityPercent: 36, shellQualityPercent: 28, wearRate: 4);

        _builder.Create(RecipeType.SwtorDoorImp01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Doors)
            .Resref("structure_0562")
            .Level(9)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_veldite", 5)
            .Component("wood", 3)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 26, frameQualityPercent: 41, shellQualityPercent: 31, wearRate: 3);

        _builder.Create(RecipeType.SwtorDoorSealedGate, SkillType.Fabrication)
            .Category(RecipeCategoryType.Doors)
            .Resref("structure_0563")
            .Level(9)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_veldite", 5)
            .Component("wood", 3)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 33, frameQualityPercent: 38, shellQualityPercent: 31, wearRate: 5);

        _builder.Create(RecipeType.SwtorDoorTatooineMetal, SkillType.Fabrication)
            .Category(RecipeCategoryType.Doors)
            .Resref("structure_0564")
            .Level(9)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_veldite", 5)
            .Component("wood", 3)
            .FabricatorParameters(frameSizePercent: 44, shellSizePercent: 26, frameQualityPercent: 35, shellQualityPercent: 34, wearRate: 3);
    }

    private void Lighting()
    {
        _builder.Create(RecipeType.SwtorLamp001, SkillType.Fabrication)
            .Category(RecipeCategoryType.Lighting)
            .Resref("structure_0673")
            .Level(9)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_veldite", 5)
            .Component("wood", 5)
            .FabricatorParameters(frameSizePercent: 15, shellSizePercent: 27, frameQualityPercent: 15, shellQualityPercent: 32, wearRate: 5);

        _builder.Create(RecipeType.SwtorLightpost01, SkillType.Fabrication)
            .Category(RecipeCategoryType.Lighting)
            .Resref("structure_0674")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 18, shellSizePercent: 32, frameQualityPercent: 15, shellQualityPercent: 30, wearRate: 4);

        _builder.Create(RecipeType.SwtorStreetLight, SkillType.Fabrication)
            .Category(RecipeCategoryType.Lighting)
            .Resref("structure_0825")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 26, frameQualityPercent: 15, shellQualityPercent: 38, wearRate: 3);
    }
}
