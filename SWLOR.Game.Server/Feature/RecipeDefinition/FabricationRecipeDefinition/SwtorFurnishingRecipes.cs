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
        _builder.Create(RecipeType.SwtorCrate03, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0546")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 28, frameQualityPercent: 36, shellQualityPercent: 38, wearRate: 7);

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

        _builder.Create(RecipeType.SwtorSafeBox, SkillType.Fabrication)
            .Category(RecipeCategoryType.MiscellaneousFurniture)
            .Resref("structure_0795")
            .Level(22)
            .EnhancementSlots(RecipeEnhancementType.Structure, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 1)
            .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 36, frameQualityPercent: 31, shellQualityPercent: 38, wearRate: 7);

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
    }

    private void Electronics()
    {
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
    }

    private void Fixtures()
    {
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
    }

    private void Flooring()
    {
        _builder.Create(RecipeType.SwtorRugBlueGeometrySmall, SkillType.Fabrication)
            .Category(RecipeCategoryType.Flooring)
            .Resref("structure_0781")
            .Level(15)
            .EnhancementSlots(RecipeEnhancementType.Structure, 1)
            .Component("fiberp_flawed", 2)
            .Component("lth_flawed", 3)
            .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 64, frameQualityPercent: 20, shellQualityPercent: 60, wearRate: 5);

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
    }
}
