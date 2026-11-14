using System.Collections.Generic;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Feature.FarmingDefinition
{
    /// <summary>
    /// The 26 crops of the Player City Revamp in five tiers. The tier sets a crop's level and grow time (5/15/25/35/45 and
    /// 12/16/20/24/24 hours). Each crop yields an item harvesting already gives: the plant items of that tier's herb and
    /// vegetable nodes (<c>HERBS_n</c>, <c>VEGETABLES_PATCH_n</c>), and the fiberplast of the scavenging bush of the tier
    /// (<c>SCAVENGING_BUSH_n</c>). Processed items from those nodes (Plant-Based Butter, Tofu, Dried Bonito) are not crops.
    /// </summary>
    public class CropListDefinition : ICropListDefinition
    {
        private readonly CropBuilder _builder = new();

        public Dictionary<CropType, CropDefinition> BuildCrops()
        {
            TierOne();
            TierTwo();
            TierThree();
            TierFour();
            TierFive();

            return _builder.Build();
        }

        private void TierOne()
        {
            _builder.Create(CropType.ViscaranOrange).Name("Viscaran Orange").Tier(1).Seed("seed_orange").Harvest("v_orange");
            _builder.Create(CropType.ViscaranLemon).Name("Viscaran Lemon").Tier(1).Seed("seed_lemon").Harvest("v_lemon");
            _builder.Create(CropType.ViscaranPebble).Name("Viscaran Pebble").Tier(1).Seed("seed_pebble").Harvest("v_pebble");
            _builder.Create(CropType.ViscaranRosemary).Name("Viscaran Rosemary").Tier(1).Seed("seed_rosemary").Harvest("herb_v");
            _builder.Create(CropType.FiberShrubRuined).Name("Fiber Shrub (Ruined)").Tier(1).Seed("seed_fiber_1").Harvest("fiberp_ruined");
        }

        private void TierTwo()
        {
            _builder.Create(CropType.ViscaranApple).Name("Viscaran Apple").Tier(2).Seed("seed_apple").Harvest("v_apple");
            _builder.Create(CropType.ViscaranPeas).Name("Viscaran Peas").Tier(2).Seed("seed_peas").Harvest("v_peas");
            _builder.Create(CropType.PassionFruit).Name("Passion Fruit").Tier(2).Seed("seed_passion").Harvest("passion_fruit");
            _builder.Create(CropType.MandalorianThyme).Name("Mandalorian Thyme").Tier(2).Seed("seed_thyme").Harvest("herb_m");
            _builder.Create(CropType.FiberShrubFlawed).Name("Fiber Shrub (Flawed)").Tier(2).Seed("seed_fiber_2").Harvest("fiberp_flawed");
        }

        private void TierThree()
        {
            _builder.Create(CropType.HutlarAcorn).Name("Hutlar Acorn").Tier(3).Seed("seed_acorn").Harvest("h_acorn");
            _builder.Create(CropType.StrangePineapple).Name("Strange Pineapple").Tier(3).Seed("seed_pineapple").Harvest("s_pineapple");
            _builder.Create(CropType.VegetableClump).Name("Vegetable Clump").Tier(3).Seed("seed_veg_clump").Harvest("veggie_clump");
            _builder.Create(CropType.MonCalanSeaweed).Name("Mon Calan Seaweed").Tier(3).Seed("seed_seaweed").Harvest("herb_c");
            _builder.Create(CropType.FiberShrubGood).Name("Fiber Shrub (Good Quality)").Tier(3).Seed("seed_fiber_3").Harvest("fiberp_good");
        }

        private void TierFour()
        {
            _builder.Create(CropType.Ginger).Name("Ginger").Tier(4).Seed("seed_ginger").Harvest("ginger");
            _builder.Create(CropType.Melon).Name("Melon").Tier(4).Seed("seed_melon").Harvest("melon");
            _builder.Create(CropType.Mushroom).Name("Mushroom").Tier(4).Seed("seed_mushroom").Harvest("mushroom");
            _builder.Create(CropType.Walnut).Name("Walnut").Tier(4).Seed("seed_walnut").Harvest("walnut");
            _builder.Create(CropType.Cornucopia).Name("Cornucopia").Tier(4).Seed("seed_cornucopia").Harvest("cornucopia");
            _builder.Create(CropType.TatooineDesertSage).Name("Tatooine Desert Sage").Tier(4).Seed("seed_desert_sage").Harvest("herb_t");
            _builder.Create(CropType.FiberShrubImperfect).Name("Fiber Shrub (Imperfect)").Tier(4).Seed("seed_fiber_4").Harvest("fiberp_imperfect");
        }

        private void TierFive()
        {
            _builder.Create(CropType.Turnip).Name("Turnip").Tier(5).Seed("seed_turnip").Harvest("turnip");
            _builder.Create(CropType.Tomato).Name("Tomato").Tier(5).Seed("seed_tomato").Harvest("tomato");
            _builder.Create(CropType.DathomirOregano).Name("Dathomir Oregano").Tier(5).Seed("seed_oregano").Harvest("herb_x");
            _builder.Create(CropType.FiberShrubHigh).Name("Fiber Shrub (High Quality)").Tier(5).Seed("seed_fiber_5").Harvest("fiberp_high");
        }
    }
}
