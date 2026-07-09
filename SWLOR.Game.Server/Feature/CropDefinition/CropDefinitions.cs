using System.Collections.Generic;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Feature.CropDefinition
{
    public class CropDefinitions : ICropListDefinition
    {
        private readonly CropBuilder _builder = new();

        public Dictionary<CropType, CropDetail> BuildCrops()
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
            _builder.Create(CropType.VeshHerb)
                .Name("Vesh Herb")
                .Description("A hardy medicinal herb used in a wide range of cooked dishes.")
                .RequiredRank(0)
                .SeedResref("seed_vesh")
                .Yield("herb_v", 6)
                .SecondsPerStage(14400);

            _builder.Create(CropType.JoganFruit)
                .Name("Jogan Fruit")
                .Description("A sweet Jogan fruit from the outer reaches, suitable for cooking or fresh consumption.")
                .RequiredRank(2)
                .SeedResref("seed_jogan")
                .PristineResref("prs_jogan")
                .Yield("jogan_fruit", 4)
                .SecondsPerStage(14400);

            _builder.Create(CropType.PebbleFruit)
                .Name("Pebble Fruit")
                .Description("A small, dense fruit commonly used in stews and sauces.")
                .RequiredRank(4)
                .SeedResref("seed_pebble")
                .Yield("v_pebble", 5)
                .SecondsPerStage(14400);

            _builder.Create(CropType.CitrusVine)
                .Name("Citrus Vine")
                .Description("Yields both orange and lemon fruit for beverages, cooking, and baking.")
                .RequiredRank(6)
                .SeedResref("seed_citrus")
                .Yield("v_orange", 3)
                .Yield("v_lemon", 3)
                .SecondsPerStage(14400);

            _builder.Create(CropType.SweetCane)
                .Name("Sweet Cane")
                .Description("A sugar-rich plant processed into refined sweetener for cooking and brewing.")
                .RequiredRank(8)
                .SeedResref("seed_cane")
                .Yield("sugar", 8)
                .SecondsPerStage(14400);
        }

        private void Tier2()
        {
            _builder.Create(CropType.MaraHerb)
                .Name("Mara Herb")
                .Description("An aromatic herb with warm flavors, essential to savory cuisine.")
                .RequiredRank(10)
                .SeedResref("seed_mara")
                .Yield("herb_m", 6)
                .SecondsPerStage(28800);

            _builder.Create(CropType.TarineTeaLeaf)
                .Name("Tarine Tea Leaf")
                .Description("Leaves from the Tarine plant dried and brewed into soothing beverages.")
                .RequiredRank(12)
                .SeedResref("seed_tarine")
                .PristineResref("prs_tarine")
                .Yield("tarine_leaf", 4)
                .SecondsPerStage(28800);

            _builder.Create(CropType.PoddedPeas)
                .Name("Podded Peas")
                .Description("Legume pods yielding tender peas for soups, side dishes, and cooking.")
                .RequiredRank(14)
                .SeedResref("seed_peas")
                .Yield("v_peas", 5)
                .SecondsPerStage(28800);

            _builder.Create(CropType.OrchardApple)
                .Name("Orchard Apple")
                .Description("Crisp, sweet apples suitable for fresh eating, pressing, or baking.")
                .RequiredRank(16)
                .SeedResref("seed_apple")
                .Yield("v_apple", 5)
                .SecondsPerStage(28800);

            _builder.Create(CropType.PassionFruit)
                .Name("Passion Fruit")
                .Description("An exotic fruit with tangy pulp used in drinks, desserts, and sauces.")
                .RequiredRank(18)
                .SeedResref("seed_passion")
                .Yield("passion_fruit", 5)
                .SecondsPerStage(28800);
        }

        private void Tier3()
        {
            _builder.Create(CropType.CassaHerb)
                .Name("Cassa Herb")
                .Description("A potent culinary herb with warming spice notes for refined dishes.")
                .RequiredRank(20)
                .SeedResref("seed_cassa")
                .Yield("herb_c", 6)
                .SecondsPerStage(43200);

            _builder.Create(CropType.NysillimGrain)
                .Name("Nysillim Grain")
                .Description("A hardy grain which mills into flour for baking or feeds hungry beasts.")
                .RequiredRank(22)
                .SeedResref("seed_nysillim")
                .PristineResref("prs_nysillim")
                .Yield("nysillim_grain", 6)
                .SecondsPerStage(43200);

            _builder.Create(CropType.VeggieCluster)
                .Name("Veggie Cluster")
                .Description("A mixed harvest of garden vegetables for diverse cooking applications.")
                .RequiredRank(24)
                .SeedResref("seed_vegclump")
                .Yield("veggie_clump", 5)
                .SecondsPerStage(43200);

            _builder.Create(CropType.Pineapple)
                .Name("Pineapple")
                .Description("A sweet tropical fruit enjoyed fresh, grilled, or in cooked preparations.")
                .RequiredRank(26)
                .SeedResref("seed_pineapple")
                .Yield("s_pineapple", 5)
                .SecondsPerStage(43200);

            _builder.Create(CropType.HardnutSapling)
                .Name("Hardnut Sapling")
                .Description("A productive tree yielding both acorns and walnuts for cooking and crafting.")
                .RequiredRank(28)
                .SeedResref("seed_hardnut")
                .Yield("h_acorn", 3)
                .Yield("walnut", 3)
                .SecondsPerStage(43200);
        }

        private void Tier4()
        {
            _builder.Create(CropType.ToshHerb)
                .Name("Tosh Herb")
                .Description("A valuable herb with complex, layered flavors for sophisticated cuisine.")
                .RequiredRank(30)
                .SeedResref("seed_tosh")
                .Yield("herb_t", 6)
                .SecondsPerStage(64800);

            _builder.Create(CropType.ShuuraFruit)
                .Name("Shuura Fruit")
                .Description("A succulent Naboo orchard fruit prized for its unique sweetness in gourmet cooking.")
                .RequiredRank(32)
                .SeedResref("seed_shuura")
                .PristineResref("prs_shuura")
                .Yield("shuura_fruit", 4)
                .SecondsPerStage(64800);

            _builder.Create(CropType.CaveMushroom)
                .Name("Cave Mushroom")
                .Description("Earthy fungal growth harvested for depth and umami in culinary preparations.")
                .RequiredRank(34)
                .SeedResref("seed_mushroom")
                .Yield("mushroom", 5)
                .SecondsPerStage(64800);

            _builder.Create(CropType.GingerRoot)
                .Name("Ginger Root")
                .Description("A pungent rhizome essential for flavoring both savory and sweet preparations.")
                .RequiredRank(36)
                .SeedResref("seed_ginger")
                .Yield("ginger", 5)
                .SecondsPerStage(64800);

            _builder.Create(CropType.ButterPlant)
                .Name("Butter Plant")
                .Description("A dual-yielding plant producing both creamy and melon produce for cooking.")
                .RequiredRank(38)
                .SeedResref("seed_butter")
                .Yield("plant_butter", 3)
                .Yield("melon", 3)
                .SecondsPerStage(64800);
        }

        private void Tier5()
        {
            _builder.Create(CropType.XenHerb)
                .Name("Xen Herb")
                .Description("A rare herb with subtle, refined flavors highly sought by master chefs.")
                .RequiredRank(40)
                .SeedResref("seed_xen")
                .Yield("herb_x", 6)
                .SecondsPerStage(86400);

            _builder.Create(CropType.Silkvine)
                .Name("Silkvine")
                .Description("A plant producing fine fiber suitable for weaving quality textiles and garments.")
                .RequiredRank(42)
                .SeedResref("seed_silkvine")
                .PristineResref("prs_silkvine")
                .Yield("silkvine_fiber", 4)
                .SecondsPerStage(86400);

            _builder.Create(CropType.HothouseTomato)
                .Name("Hothouse Tomato")
                .Description("Dual-yielding vegetables yielding tomatoes and roots for sophisticated cooking.")
                .RequiredRank(44)
                .SeedResref("seed_tomato")
                .Yield("tomato", 3)
                .Yield("turnip", 3)
                .SecondsPerStage(86400);

            _builder.Create(CropType.MeiloorunMelon)
                .Name("Meiloorun Melon")
                .Description("A rare, sweet melon notoriously difficult to cultivate, reserved for fine dining.")
                .RequiredRank(46)
                .SeedResref("seed_meiloorun")
                .PristineResref("prs_meiloorun")
                .Yield("meiloorun", 4)
                .SecondsPerStage(86400);

            _builder.Create(CropType.GoldenCornucopia)
                .Name("Golden Cornucopia")
                .Description("A legendary plant yielding abundant cornucopia fruit for the finest cuisine.")
                .RequiredRank(48)
                .SeedResref("seed_cornucopia")
                .Yield("cornucopia", 5)
                .SecondsPerStage(86400);

            _builder.Create(CropType.FelucianFirepepper)
                .Name("Felucian Firepepper")
                .Description("An incendiary pepper native to Felucia that adds bold, spicy heat to dishes.")
                .RequiredRank(50)
                .SeedResref("seed_firepepper")
                .PristineResref("prs_firepepper")
                .Yield("firepepper", 3)
                .SecondsPerStage(86400);
        }
    }
}
