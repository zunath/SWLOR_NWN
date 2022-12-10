using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class RingRecipes : IRecipeListDefinition
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
            // Battlemaster Ring
            _builder.Create(RecipeType.BattlemasterRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("bm_ring")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 1)
                .Component("jade", 1);

            // Spiritmaster Ring
            _builder.Create(RecipeType.SpiritmasterRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sm_ring")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 1)
                .Component("jade", 1);

            // Combat Ring
            _builder.Create(RecipeType.CombatRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("com_ring")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 1)
                .Component("jade", 1);

            // Advent Ring
            _builder.Create(RecipeType.AdventRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("advent_ring")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Ring
            _builder.Create(RecipeType.AmateurRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Ring
            _builder.Create(RecipeType.ClothRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Ring
            _builder.Create(RecipeType.ChefRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);
        }

        private void Tier2()
        {
            // Titan Ring
            _builder.Create(RecipeType.TitanRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tit_ring")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 1)
                .Component("agate", 1);

            // Vivid Ring
            _builder.Create(RecipeType.VividRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("viv_ring")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 1)
                .Component("agate", 1);

            // Valor Ring
            _builder.Create(RecipeType.ValorRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("val_ring")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 1)
                .Component("agate", 1);

            // Frontier Ring
            _builder.Create(RecipeType.FrontierRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("frontier_ring")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Ring
            _builder.Create(RecipeType.WorkerRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Ring
            _builder.Create(RecipeType.LinenRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Ring
            _builder.Create(RecipeType.VelveteenRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);
        }

        private void Tier3()
        {
            // Quark Ring
            _builder.Create(RecipeType.QuarkRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("qk_ring")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 1)
                .Component("citrine", 1);

            // Reginal Ring
            _builder.Create(RecipeType.ReginalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("reg_ring")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 1)
                .Component("citrine", 1);

            // Forza Ring
            _builder.Create(RecipeType.ForzaRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("for_ring")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 1)
                .Component("citrine", 1);

            // Majestic Ring
            _builder.Create(RecipeType.MajesticRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("majestic_ring")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Mechanic Ring
            _builder.Create(RecipeType.MechanicRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Ring
            _builder.Create(RecipeType.DesignerRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Ring
            _builder.Create(RecipeType.SilkRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);
        }

        private void Tier4()
        {
            // Argos Ring
            _builder.Create(RecipeType.ArgosRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ar_ring")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 1)
                .Component("ruby", 1);

            // Grenada Ring
            _builder.Create(RecipeType.GrenadaRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("gr_ring")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 1)
                .Component("ruby", 1);

            // Survival Ring
            _builder.Create(RecipeType.SurvivalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sur_ring")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 1)
                .Component("ruby", 1);

            // Dream Ring
            _builder.Create(RecipeType.DreamRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("majestic_ring001")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Devotion Ring
            _builder.Create(RecipeType.DevotionRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Ring
            _builder.Create(RecipeType.OasisRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Ring
            _builder.Create(RecipeType.VintageRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);
        }

        private void Tier5()
        {
            // Eclipse Ring
            _builder.Create(RecipeType.EclipseRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ec_ring")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 1)
                .Component("emerald", 1);

            // Transcendent Ring
            _builder.Create(RecipeType.TranscendentRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tran_ring")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 1)
                .Component("emerald", 1);

            // Supreme Ring
            _builder.Create(RecipeType.SupremeRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sup_ring")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 1)
                .Component("emerald", 1);

            // Eternal Ring
            _builder.Create(RecipeType.EternalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("eterenal_ring")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Ring
            _builder.Create(RecipeType.SkysteelRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Ring
            _builder.Create(RecipeType.RoseRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Ring
            _builder.Create(RecipeType.MoonflameRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Ring
            _builder.Create(RecipeType.ChaosRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ch_ring")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 20)
                .Component("emerald", 20)
                .Component("chiro_shard", 1);

            // Magus Ring
            _builder.Create(RecipeType.MagusRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("mag_ring")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 20)
                .Component("emerald", 20)
                .Component("chiro_shard", 1);

            // Immortal Ring
            _builder.Create(RecipeType.ImmortalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("imm_ring")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 20)
                .Component("emerald", 20)
                .Component("chiro_shard", 1);
        }

    }
}