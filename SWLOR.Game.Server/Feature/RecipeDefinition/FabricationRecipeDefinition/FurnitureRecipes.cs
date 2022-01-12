using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class FurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

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
				.Category(RecipeCategoryType.Beds)
				.Resref("structure_0085")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 2)
				.Component("fiberp_ruined", 1);

			// Easel
			_builder.Create(RecipeType.Easel, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0045")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fiberp_ruined", 2)
				.Component("lth_ruined", 1);

			// Candle
			_builder.Create(RecipeType.Candle, SkillType.Fabrication)
				.Category(RecipeCategoryType.Lighting)
				.Resref("structure_0062")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 3)
				.Component("fiberp_ruined", 1);

			// Carpet
			_builder.Create(RecipeType.Carpet, SkillType.Fabrication)
				.Category(RecipeCategoryType.Flooring)
				.Resref("structure_0077")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 3)
				.Component("fiberp_ruined", 1);

			// Cot
			_builder.Create(RecipeType.Cot, SkillType.Fabrication)
				.Category(RecipeCategoryType.Beds)
				.Resref("structure_0069")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 4)
				.Component("fiberp_ruined", 2);

			// Keg
			_builder.Create(RecipeType.Keg, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0047")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 4)
				.Component("ref_veldite", 2);

			// Chair, Wood, Small
			_builder.Create(RecipeType.ChairWoodSmall, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0120")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 4)
				.Component("ref_veldite", 2);

			// Rope Coil
			_builder.Create(RecipeType.RopeCoil, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0023")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fiberp_ruined", 5)
				.Component("lth_ruined", 2);

			// Throw Rug
			_builder.Create(RecipeType.ThrowRug, SkillType.Fabrication)
				.Category(RecipeCategoryType.Flooring)
				.Resref("structure_0072")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 5)
				.Component("fiberp_ruined", 2);

			// Chair, Wood
			_builder.Create(RecipeType.ChairWood, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0119")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 5)
				.Component("ref_veldite", 2);

			// Cushions
			_builder.Create(RecipeType.Cushions, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0061")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 6)
				.Component("fiberp_ruined", 3);

			// Table, Wood
			_builder.Create(RecipeType.TableWood, SkillType.Fabrication)
				.Category(RecipeCategoryType.Surfaces)
				.Resref("structure_0070")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 6)
				.Component("ref_veldite", 3);

			// Bench, Wood, Small
			_builder.Create(RecipeType.BenchWoodSmall, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0118")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 6)
				.Component("ref_veldite", 3);

			// Table, Wood, With Fish
			_builder.Create(RecipeType.TableWoodWithFish, SkillType.Fabrication)
				.Category(RecipeCategoryType.Surfaces)
				.Resref("structure_0057")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 7)
				.Component("ref_veldite", 3);

			// Altar, Hand
			_builder.Create(RecipeType.AltarHand, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0059")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_veldite", 7)
				.Component("wood", 3);

			// Footstool
			_builder.Create(RecipeType.Footstool, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0107")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 7)
				.Component("ref_veldite", 3);

			// Pedestal
			_builder.Create(RecipeType.Pedestal, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0022")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 8)
				.Component("ref_veldite", 4);

			// Tome
			_builder.Create(RecipeType.Tome, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0080")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 8)
				.Component("fiberp_ruined", 4);

			// Potted Plant
			_builder.Create(RecipeType.PottedPlant, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0091")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 8)
				.Component("ref_veldite", 4);

			// Net
			_builder.Create(RecipeType.Net, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0051")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("lth_ruined", 9)
				.Component("fiberp_ruined", 4);

			// Gong
			_builder.Create(RecipeType.Gong, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0013")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_veldite", 9)
				.Component("wood", 4);

			// Doorway, Metal
			_builder.Create(RecipeType.DoorwayMetal, SkillType.Fabrication)
				.Category(RecipeCategoryType.Doors)
				.Resref("structure_0019")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_veldite", 10)
				.Component("wood", 5);

			// Bird Cage
			_builder.Create(RecipeType.BirdCage, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0081")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_veldite", 10)
				.Component("wood", 5);

			// Torch Bracket
			_builder.Create(RecipeType.TorchBracket, SkillType.Fabrication)
				.Category(RecipeCategoryType.Lighting)
				.Resref("structure_0039")
				.Level(10)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_veldite", 11)
				.Component("wood", 6);

			// Obelisk, Small
			_builder.Create(RecipeType.ObeliskSmall, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0006")
				.Level(10)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_veldite", 11)
				.Component("wood", 6);

			// Table, Wood, Large
			_builder.Create(RecipeType.TableWoodLarge, SkillType.Fabrication)
				.Category(RecipeCategoryType.Surfaces)
				.Resref("structure_0056")
				.Level(10)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("wood", 11)
				.Component("ref_veldite", 6);

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
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 2)
				.Component("ref_scordspar", 1);

			// Birdbath
			_builder.Create(RecipeType.Birdbath, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0026")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 2)
				.Component("lth_flawed", 1);

			// Bench, Wood
			_builder.Create(RecipeType.BenchWood, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0113")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 2)
				.Component("ref_scordspar", 1);

			// Ladder, Dark
			_builder.Create(RecipeType.LadderDark, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0008")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 3)
				.Component("ref_scordspar", 2);

			// Pillar, Wood, Dark
			_builder.Create(RecipeType.PillarWoodDark, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0082")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 3)
				.Component("ref_scordspar", 2);

			// Window
			_builder.Create(RecipeType.Window, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0060")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 4)
				.Component("ref_scordspar", 2);

			// Pedestal, Evil
			_builder.Create(RecipeType.PedestalEvil, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0025")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 4)
				.Component("lth_flawed", 2);

			// Brazier, Round
			_builder.Create(RecipeType.BrazierRound, SkillType.Fabrication)
				.Category(RecipeCategoryType.Lighting)
				.Resref("structure_0021")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 5)
				.Component("lth_flawed", 3);

			// Vase, Tall
			_builder.Create(RecipeType.VaseTall, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0085")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 5)
				.Component("fine_wood", 3);

			// Brazier, Stone
			_builder.Create(RecipeType.BrazierStone, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0017")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 6)
				.Component("fine_wood", 3);

			// Ottoman
			_builder.Create(RecipeType.Ottoman, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0086")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 6)
				.Component("lth_flawed", 3);

			// Statue, Guardian
			_builder.Create(RecipeType.StatueGuardian, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0018")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 7)
				.Component("fine_wood", 4);

			// Lamp Post
			_builder.Create(RecipeType.LampPost, SkillType.Fabrication)
				.Category(RecipeCategoryType.Lighting)
				.Resref("structure_0038")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 7)
				.Component("fiberp_flawed", 4);

			// Flaming Statue
			_builder.Create(RecipeType.FlamingStatue, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0020")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 8)
				.Component("fine_wood", 4);

			// Pillar, Rounded
			_builder.Create(RecipeType.PillarRounded, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0088")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 8)
				.Component("fine_wood", 4);

			// Jukebox
			_builder.Create(RecipeType.Jukebox, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0005")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("elec_flawed", 9)
				.Component("ref_scordspar", 5);

			// Painting 1
			_builder.Create(RecipeType.Painting1, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0092")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 9)
				.Component("fiberp_flawed", 5);

			// Doorway, Stone
			_builder.Create(RecipeType.DoorwayStone, SkillType.Fabrication)
				.Category(RecipeCategoryType.Doors)
				.Resref("structure_0030")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 10)
				.Component("fine_wood", 5);

			// Pillar, Wood
			_builder.Create(RecipeType.PillarWood, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0040")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 10)
				.Component("fiberp_flawed", 5);

			// Statue, Monster
			_builder.Create(RecipeType.StatueMonster, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0014")
				.Level(20)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("ref_scordspar", 11)
				.Component("lth_flawed", 6);

			// Candelabra
			_builder.Create(RecipeType.Candelabra, SkillType.Fabrication)
				.Category(RecipeCategoryType.Lighting)
				.Resref("structure_0090")
				.Level(20)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 1)
				.Component("fine_wood", 11)
				.Component("fiberp_flawed", 6);

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
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 2)
				.Component("ancient_wood", 1);

			// Gnomish Contraption
			_builder.Create(RecipeType.GnomishContraption, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0032")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 2)
				.Component("ancient_wood", 1);

			// Female Statue
			_builder.Create(RecipeType.FemaleStatue, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0031")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 2)
				.Component("lth_good", 1);

			// Vase, Rounded
			_builder.Create(RecipeType.VaseRounded, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0076")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 3)
				.Component("lth_good", 2);

			// Pedestal, Sword
			_builder.Create(RecipeType.PedestalSword, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0029")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 3)
				.Component("ancient_wood", 2);

			// Bed, Wood, Yellow
			_builder.Create(RecipeType.BedWoodYellow, SkillType.Fabrication)
				.Category(RecipeCategoryType.Beds)
				.Resref("structure_0078")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ancient_wood", 4)
				.Component("fiberp_good", 2);

			// Urn
			_builder.Create(RecipeType.Urn, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0067")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 4)
				.Component("ancient_wood", 2);

			// Pillar, Stone
			_builder.Create(RecipeType.PillarStone, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0033")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 5)
				.Component("ancient_wood", 3);

			// Cage
			_builder.Create(RecipeType.Cage, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0037")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 5)
				.Component("ancient_wood", 3);

			// Overgrown Pillar
			_builder.Create(RecipeType.OvergrownPillar, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0079")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 6)
				.Component("ancient_wood", 3);

			// Floor-anchored shackles
			_builder.Create(RecipeType.FloorAnchoredShackles, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0100")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 6)
				.Component("lth_good", 3);

			// Statue, Wyvern
			_builder.Create(RecipeType.StatueWyvern, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0024")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 7)
				.Component("lth_good", 4);

			// Bunk Bed
			_builder.Create(RecipeType.BunkBed, SkillType.Fabrication)
				.Category(RecipeCategoryType.Beds)
				.Resref("structure_0083")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ancient_wood", 7)
				.Component("fiberp_good", 4);

			// Fountain
			_builder.Create(RecipeType.Fountain, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0043")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 8)
				.Component("fiberp_good", 4);

			// Round Wooden Table
			_builder.Create(RecipeType.RoundWoodenTable, SkillType.Fabrication)
				.Category(RecipeCategoryType.Surfaces)
				.Resref("structure_0101")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ancient_wood", 8)
				.Component("fiberp_good", 4);

			// Altar, Stone
			_builder.Create(RecipeType.AltarStone, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0068")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 9)
				.Component("lth_good", 5);

			// Statue of Lathander
			_builder.Create(RecipeType.StatueOfLathander, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0035")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 9)
				.Component("lth_good", 5);

			// Bear Skin Rug
			_builder.Create(RecipeType.BearSkinRug, SkillType.Fabrication)
				.Category(RecipeCategoryType.Flooring)
				.Resref("structure_0064")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("fiberp_good", 10)
				.Component("lth_good", 5);

			// Carpet, Fancy
			_builder.Create(RecipeType.CarpetFancy, SkillType.Fabrication)
				.Category(RecipeCategoryType.Flooring)
				.Resref("structure_0093")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("lth_good", 10)
				.Component("fiberp_good", 5);

			// Altar, Evil
			_builder.Create(RecipeType.AltarEvil, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0055")
				.Level(30)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_plagionite", 11)
				.Component("lth_good", 6);

			// Illithid Table
			_builder.Create(RecipeType.IllithidTable, SkillType.Fabrication)
				.Category(RecipeCategoryType.Surfaces)
				.Resref("structure_0095")
				.Level(30)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ancient_wood", 11)
				.Component("ref_plagionite", 6);

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
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 2)
				.Component("aracia_wood", 1);

			// Bed, Large
			_builder.Create(RecipeType.BedLarge, SkillType.Fabrication)
				.Category(RecipeCategoryType.Beds)
				.Resref("structure_0075")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("aracia_wood", 2)
				.Component("ref_keromber", 1);

			// Statue, Wizard
			_builder.Create(RecipeType.StatueWizard, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0012")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 3)
				.Component("fiberp_imperfect", 2);

			// Bench, Large
			_builder.Create(RecipeType.BenchLarge, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0115")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("aracia_wood", 3)
				.Component("ref_keromber", 2);

			// Table, Stone, Large
			_builder.Create(RecipeType.TableStoneLarge, SkillType.Fabrication)
				.Category(RecipeCategoryType.Surfaces)
				.Resref("structure_0073")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 4)
				.Component("aracia_wood", 2);

			// Carpet, Round, Blue
			_builder.Create(RecipeType.CarpetRoundBlue, SkillType.Fabrication)
				.Category(RecipeCategoryType.Flooring)
				.Resref("structure_0054")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("fiberp_imperfect", 4)
				.Component("lth_imperfect", 2);

			// Chandelier
			_builder.Create(RecipeType.Chandelier, SkillType.Fabrication)
				.Category(RecipeCategoryType.Lighting)
				.Resref("structure_0065")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 5)
				.Component("aracia_wood", 3);

			// Chair, Wood, Medium
			_builder.Create(RecipeType.ChairWoodMedium, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0114")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("aracia_wood", 5)
				.Component("ref_keromber", 3);

			// Statue, Huge
			_builder.Create(RecipeType.StatueHuge, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0009")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 6)
				.Component("aracia_wood", 3);

			// Bench, Stone, Dwarven
			_builder.Create(RecipeType.BenchStoneDwarven, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0111")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 6)
				.Component("aracia_wood", 3);

			// Pillar, Flame
			_builder.Create(RecipeType.PillarFlame, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0036")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 7)
				.Component("aracia_wood", 4);

			// Drow Table
			_builder.Create(RecipeType.DrowTable, SkillType.Fabrication)
				.Category(RecipeCategoryType.Surfaces)
				.Resref("structure_0108")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("aracia_wood", 7)
				.Component("ref_keromber", 4);

			// Mining Well Platform
			_builder.Create(RecipeType.MiningWellPlatform, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0028")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 8)
				.Component("aracia_wood", 4);

			// Mining Well
			_builder.Create(RecipeType.MiningWell, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0010")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 9)
				.Component("aracia_wood", 5);

			// Bed, Stone, Yellow
			_builder.Create(RecipeType.BedStoneYellow, SkillType.Fabrication)
				.Category(RecipeCategoryType.Beds)
				.Resref("structure_0074")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 9)
				.Component("aracia_wood", 5);

			// Mirror
			_builder.Create(RecipeType.Mirror, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0106")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 10)
				.Component("aracia_wood", 5);

			// Bed, Extra Large
			_builder.Create(RecipeType.BedExtraLarge, SkillType.Fabrication)
				.Category(RecipeCategoryType.Beds)
				.Resref("structure_0052")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("aracia_wood", 10)
				.Component("ref_keromber", 5);

			// Rune Pillar
			_builder.Create(RecipeType.RunePillar, SkillType.Fabrication)
				.Category(RecipeCategoryType.Fixtures)
				.Resref("structure_0104")
				.Level(40)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 11)
				.Component("aracia_wood", 6);

			// Statue, Cyric
			_builder.Create(RecipeType.StatueCyric, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0041")
				.Level(40)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_keromber", 11)
				.Component("aracia_wood", 6);

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
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_jasioclase", 2)
				.Component("hyphae_wood", 1);

			// Dartboard
			_builder.Create(RecipeType.Dartboard, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0098")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 2)
				.Component("lth_high", 1);

			// Dran Statue
			_builder.Create(RecipeType.DranStatue, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0048")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_jasioclase", 3)
				.Component("hyphae_wood", 2);

			// Map
			_builder.Create(RecipeType.Map, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0099")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("fiberp_high", 3)
				.Component("lth_high", 2);

			// Sea Idol
			_builder.Create(RecipeType.SeaIdol, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0042")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_jasioclase", 4)
				.Component("fiberp_high", 2);

			// Painting 2
			_builder.Create(RecipeType.Painting2, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0089")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 4)
				.Component("ref_jasioclase", 2);

			// Chair, Stone
			_builder.Create(RecipeType.ChairStone, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0122")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_jasioclase", 5)
				.Component("hyphae_wood", 3);

			// Drow Altar
			_builder.Create(RecipeType.DrowAltar, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0097")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_jasioclase", 5)
				.Component("hyphae_wood", 3);

			// Throne, Wood
			_builder.Create(RecipeType.ThroneWood, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0121")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 6)
				.Component("ref_jasioclase", 3);

			// Illithid Chair
			_builder.Create(RecipeType.IllithidChair, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0112")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 6)
				.Component("ref_jasioclase", 3);

			// Monster Statue
			_builder.Create(RecipeType.MonsterStatue, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0044")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_jasioclase", 7)
				.Component("hyphae_wood", 4);

			// Shrine of Umberlee
			_builder.Create(RecipeType.ShrineOfUmberlee, SkillType.Fabrication)
				.Category(RecipeCategoryType.Statues)
				.Resref("structure_0103")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("ref_jasioclase", 7)
				.Component("hyphae_wood", 4);

			// Carpet, Fancy, Smaller
			_builder.Create(RecipeType.CarpetFancySmaller, SkillType.Fabrication)
				.Category(RecipeCategoryType.Flooring)
				.Resref("structure_0096")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("lth_high", 8)
				.Component("fiberp_high", 4);

			// Drow Chair
			_builder.Create(RecipeType.DrowChair, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0116")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 8)
				.Component("ref_jasioclase", 4);

			// Bench, Wood, Small 2
			_builder.Create(RecipeType.BenchWoodSmall2, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0123")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 9)
				.Component("ref_jasioclase", 5);

			// Drow Bar
			_builder.Create(RecipeType.DrowBar, SkillType.Fabrication)
				.Category(RecipeCategoryType.MiscellaneousFurniture)
				.Resref("structure_0102")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 9)
				.Component("ref_jasioclase", 5);

			// Chair, Shell
			_builder.Create(RecipeType.ChairShell, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0124")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 10)
				.Component("ref_jasioclase", 5);

			// Bench, Wood, Large
			_builder.Create(RecipeType.BenchWoodLarge, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0117")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 10)
				.Component("ref_jasioclase", 5);

			// Couch, Wood, Yellow
			_builder.Create(RecipeType.CouchWoodYellow, SkillType.Fabrication)
				.Category(RecipeCategoryType.Seating)
				.Resref("structure_0125")
				.Level(50)
				.Quantity(1)
				.RequirementPerk(PerkType.FurnitureBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Furniture, 2)
				.Component("hyphae_wood", 11)
				.Component("lth_high", 6);
        }

	}
}