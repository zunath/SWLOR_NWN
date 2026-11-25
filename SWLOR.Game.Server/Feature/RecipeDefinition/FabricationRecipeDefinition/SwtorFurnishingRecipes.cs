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
        Electronics();
        Fixtures();
        Surfaces();
        Seating();
        Flooring();
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

        _builder.Create(RecipeType.SwtorBedBunkOpenFrame, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0438")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 4)
            .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 26, frameQualityPercent: 58, shellQualityPercent: 20, wearRate: 4);

        _builder.Create(RecipeType.SwtorBedShipBunkDark, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0439")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 4)
            .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 25, frameQualityPercent: 55, shellQualityPercent: 21, wearRate: 5);

        _builder.Create(RecipeType.SwtorBedShipBunkLight, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0440")
            .Level(13)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_scordspar", 2)
            .Component("lth_flawed", 4)
            .FabricatorParameters(frameSizePercent: 52, shellSizePercent: 30, frameQualityPercent: 59, shellQualityPercent: 20, wearRate: 6);

        _builder.Create(RecipeType.SwtorBedSingleCream, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0441")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("lth_ruined", 2)
            .Component("fiberp_ruined", 1)
            .FabricatorParameters(frameSizePercent: 57, shellSizePercent: 27, frameQualityPercent: 55, shellQualityPercent: 27, wearRate: 3);

        _builder.Create(RecipeType.SwtorBedDoubleNavy, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0442")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 51, shellSizePercent: 29, frameQualityPercent: 56, shellQualityPercent: 25, wearRate: 4);

        _builder.Create(RecipeType.SwtorBedDoubleBronzeFrame, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0443")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 55, shellSizePercent: 27, frameQualityPercent: 64, shellQualityPercent: 20, wearRate: 3);

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

        _builder.Create(RecipeType.SwtorBedDoubleGreyStripe, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0446")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 56, shellSizePercent: 27, frameQualityPercent: 61, shellQualityPercent: 20, wearRate: 3);

        _builder.Create(RecipeType.SwtorBedJediStone, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0447")
            .Level(38)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 5)
            .Component("aracia_wood", 3)
            .FabricatorParameters(frameSizePercent: 58, shellSizePercent: 27, frameQualityPercent: 55, shellQualityPercent: 27, wearRate: 8);

        _builder.Create(RecipeType.SwtorBedMandalorianOlive, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0448")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("fiberp_flawed", 2)
            .FabricatorParameters(frameSizePercent: 55, shellSizePercent: 30, frameQualityPercent: 60, shellQualityPercent: 23, wearRate: 5);

        _builder.Create(RecipeType.SwtorBedFourPosterBlueMetal, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0449")
            .Level(14)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 3)
            .Component("ref_scordspar", 2)
            .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 31, frameQualityPercent: 63, shellQualityPercent: 21, wearRate: 6);

        _builder.Create(RecipeType.SwtorBedWoodenFrameTeal, SkillType.Fabrication)
            .Category(RecipeCategoryType.Bed)
            .Resref("structure_0450")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ancient_wood", 2)
            .Component("fiberp_good", 1)
            .FabricatorParameters(frameSizePercent: 52, shellSizePercent: 28, frameQualityPercent: 61, shellQualityPercent: 20, wearRate: 6);
    }

    private void MiscellaneousFurniture()
    {
        _builder.Create(RecipeType.SwtorCrateReinforcedYellowTrim, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0546")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 33, frameQualityPercent: 31, shellQualityPercent: 30, wearRate: 6);

        _builder.Create(RecipeType.SwtorLockerMetalGreen, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0675")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 32, frameQualityPercent: 29, shellQualityPercent: 31, wearRate: 6);

        _builder.Create(RecipeType.SwtorLockerMetalGrey, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0676")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 28, frameQualityPercent: 35, shellQualityPercent: 28, wearRate: 6);

        _builder.Create(RecipeType.SwtorLockerMetalOrange, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0677")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 36, frameQualityPercent: 39, shellQualityPercent: 34, wearRate: 6);

        _builder.Create(RecipeType.SwtorSafeCompactMetal, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0795")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 30, frameQualityPercent: 35, shellQualityPercent: 30, wearRate: 7);

        _builder.Create(RecipeType.SwtorTrashCanRoundTealLights, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0862")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 30, frameQualityPercent: 35, shellQualityPercent: 28, wearRate: 4);

        _builder.Create(RecipeType.SwtorTrashCanMetalChute, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0863")
            .Level(3)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("wood", 2)
            .Component("ref_veldite", 1)
            .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 33, frameQualityPercent: 34, shellQualityPercent: 33, wearRate: 3);
    }

    private void Electronics()
    {
        _builder.Create(RecipeType.SwtorJukeboxTwinHornRed, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0659")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 20, frameQualityPercent: 16, shellQualityPercent: 22, wearRate: 5);

        _builder.Create(RecipeType.SwtorJukeboxTwinHornBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0660")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 25, frameQualityPercent: 25, shellQualityPercent: 24, wearRate: 5);

        _builder.Create(RecipeType.SwtorJukeboxArchedSilver, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0661")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 21, frameQualityPercent: 20, shellQualityPercent: 28, wearRate: 4);

        _builder.Create(RecipeType.SwtorJukeboxArchedBlueGold, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0662")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 22, frameQualityPercent: 22, shellQualityPercent: 21, wearRate: 6);

        _builder.Create(RecipeType.SwtorJukeboxArchedSilverGreen, SkillType.Fabrication)
            .Category(RecipeCategoryType.Electronics)
            .Resref("structure_0663")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("elec_flawed", 5)
            .Component("ref_scordspar", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 21, frameQualityPercent: 19, shellQualityPercent: 22, wearRate: 6);
    }

    private void Fixtures()
    {
        _builder.Create(RecipeType.SwtorBookshelfJediArchiveRed, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0653")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 24, frameQualityPercent: 29, shellQualityPercent: 27, wearRate: 7);

        _builder.Create(RecipeType.SwtorBookshelfJediArchiveGreen, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0654")
            .Level(43)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_jasioclase", 4)
            .Component("fiberp_high", 4)
            .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 21, frameQualityPercent: 33, shellQualityPercent: 26, wearRate: 9);

        _builder.Create(RecipeType.SwtorPosterSandwichAdBrown, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0761")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 21, frameQualityPercent: 33, shellQualityPercent: 23, wearRate: 4);

        _builder.Create(RecipeType.SwtorPosterSandwichAdPurple, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0762")
            .Level(18)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 5)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 25, frameQualityPercent: 32, shellQualityPercent: 27, wearRate: 5);

        _builder.Create(RecipeType.SwtorCabinetDisplayNarrow, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0934")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 2)
            .Component("fiberp_imperfect", 2)
            .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 29, frameQualityPercent: 25, shellQualityPercent: 20, wearRate: 6);

        _builder.Create(RecipeType.SwtorCabinetDisplayWide, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0935")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 2)
            .Component("fiberp_imperfect", 2)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 30, frameQualityPercent: 28, shellQualityPercent: 22, wearRate: 6);

        _builder.Create(RecipeType.SwtorCabinetDisplayStocked, SkillType.Fabrication)
            .Category(RecipeCategoryType.Fixtures)
            .Resref("structure_0936")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_keromber", 2)
            .Component("fiberp_imperfect", 2)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 27, frameQualityPercent: 30, shellQualityPercent: 26, wearRate: 8);
    }

    private void Surfaces()
    {
        _builder.Create(RecipeType.SwtorTableCantinaLarge, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0826")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 49, frameQualityPercent: 41, shellQualityPercent: 43, wearRate: 8);

        _builder.Create(RecipeType.SwtorTableCantinaSetFourSeats, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0827")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 43, frameQualityPercent: 37, shellQualityPercent: 46, wearRate: 6);

        _builder.Create(RecipeType.SwtorTableCantinaSmall, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0828")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 46, frameQualityPercent: 38, shellQualityPercent: 44, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableCantinaSetTwoSeats, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0829")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 40, frameQualityPercent: 42, shellQualityPercent: 40, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableIlluminatedRound, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0837")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 37, shellSizePercent: 40, frameQualityPercent: 41, shellQualityPercent: 43, wearRate: 8);

        _builder.Create(RecipeType.SwtorTableIlluminatedStoolsWithDrinks, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0838")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 44, frameQualityPercent: 40, shellQualityPercent: 44, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableIlluminatedStools, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0839")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 45, frameQualityPercent: 40, shellQualityPercent: 41, wearRate: 7);

        _builder.Create(RecipeType.SwtorTableRectangularWithDrinks, SkillType.Fabrication)
            .Category(RecipeCategoryType.Surfaces)
            .Resref("structure_0840")
            .Level(23)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 3)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 40, frameQualityPercent: 41, shellQualityPercent: 41, wearRate: 7);
    }

    private void Seating()
    {
        _builder.Create(RecipeType.SwtorBenchAngledTan, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0483")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("aracia_wood", 2)
            .Component("ref_keromber", 1)
            .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 35, frameQualityPercent: 44, shellQualityPercent: 36, wearRate: 6);

        _builder.Create(RecipeType.SwtorBenchSegmentedDarkGrey, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0484")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("aracia_wood", 2)
            .Component("ref_keromber", 1)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 43, frameQualityPercent: 42, shellQualityPercent: 39, wearRate: 7);

        _builder.Create(RecipeType.SwtorBenchSegmentedBlueGrey, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0485")
            .Level(32)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("aracia_wood", 2)
            .Component("ref_keromber", 1)
            .FabricatorParameters(frameSizePercent: 42, shellSizePercent: 43, frameQualityPercent: 46, shellQualityPercent: 39, wearRate: 7);

        _builder.Create(RecipeType.SwtorChairLeatherRoundedBack, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0506")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 40, frameQualityPercent: 41, shellQualityPercent: 38, wearRate: 4);

        _builder.Create(RecipeType.SwtorChairLeatherStraightBack, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0507")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 47, shellSizePercent: 35, frameQualityPercent: 46, shellQualityPercent: 35, wearRate: 5);

        _builder.Create(RecipeType.SwtorChairPlasteelRepublic, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0508")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 44, frameQualityPercent: 48, shellQualityPercent: 35, wearRate: 5);

        _builder.Create(RecipeType.SwtorChairPlasteelCantileverTan, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0509")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 46, shellSizePercent: 39, frameQualityPercent: 42, shellQualityPercent: 39, wearRate: 4);

        _builder.Create(RecipeType.SwtorChairRingBackOlive, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0510")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 39, frameQualityPercent: 41, shellQualityPercent: 43, wearRate: 4);

        _builder.Create(RecipeType.SwtorChairShellRose, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0511")
            .Level(20)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 6)
            .Component("fiberp_flawed", 3)
            .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 35, frameQualityPercent: 42, shellQualityPercent: 41, wearRate: 4);

        _builder.Create(RecipeType.SwtorCouchSteelFrameBlack, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0544")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 39, frameQualityPercent: 45, shellQualityPercent: 35, wearRate: 5);

        _builder.Create(RecipeType.SwtorCouchCurvedRed, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0816")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 35, frameQualityPercent: 45, shellQualityPercent: 40, wearRate: 3);

        _builder.Create(RecipeType.SwtorCouchSectionalBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0817")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 42, shellSizePercent: 35, frameQualityPercent: 40, shellQualityPercent: 38, wearRate: 4);

        _builder.Create(RecipeType.SwtorCouchLongRedCream, SkillType.Fabrication)
            .Category(RecipeCategoryType.Seating)
            .Resref("structure_0818")
            .Level(16)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fine_wood", 4)
            .Component("lth_flawed", 6)
            .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 43, frameQualityPercent: 43, shellQualityPercent: 40, wearRate: 5);
    }

    private void Flooring()
    {
        _builder.Create(RecipeType.SwtorRugOctagonalBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0781")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 57, frameQualityPercent: 22, shellQualityPercent: 61, wearRate: 3);

        _builder.Create(RecipeType.SwtorRugOrnateBorderRed, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0789")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 61, frameQualityPercent: 30, shellQualityPercent: 55, wearRate: 5);

        _builder.Create(RecipeType.SwtorRugFloralBorderTeal, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0790")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 61, frameQualityPercent: 20, shellQualityPercent: 60, wearRate: 4);

        _builder.Create(RecipeType.SwtorRugAngularStripeRed, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0791")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 56, frameQualityPercent: 22, shellQualityPercent: 55, wearRate: 5);

        _builder.Create(RecipeType.SwtorRugAngularStripeBlue, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0792")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 55, frameQualityPercent: 20, shellQualityPercent: 60, wearRate: 4);

        _builder.Create(RecipeType.SwtorRugGoldFiligree, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0793")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 56, frameQualityPercent: 20, shellQualityPercent: 57, wearRate: 4);
    }

    private void Lighting()
    {
        _builder.Create(RecipeType.SwtorLampAngledDesk, SkillType.Fabrication)
            .Category(RecipeCategoryType.Lighting)
            .Resref("structure_0673")
            .Level(9)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("ref_veldite", 5)
            .Component("wood", 5)
            .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 32, frameQualityPercent: 15, shellQualityPercent: 37, wearRate: 5);
    }
}
