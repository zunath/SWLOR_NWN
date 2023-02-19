using System;

namespace SWLOR.Game.Server.Service.FishingService
{
    public enum FishingRodType
    {
        [FishingRod("")]
        Invalid = 0,
        [FishingRod("bamboo_rod")]
        Bamboo = 1,
        [FishingRod("carbon_rod")]
        Carbon = 2,
        [FishingRod("clothespole")]
        Clothespole = 3,
        [FishingRod("composite_rod")]
        Composite = 4,
        [FishingRod("ebisu_rod")]
        Ebisu = 5,
        [FishingRod("fastwater_rod")]
        Fastwater = 6,
        [FishingRod("glassfiber_rod")]
        GlassFiber = 7,
        [FishingRod("halcyon_rod")]
        Halcyon = 8,
        [FishingRod("judge_rod")]
        Judge = 9,
        [FishingRod("mazemong_rod")]
        MazeMonger = 10,
        [FishingRod("willow_rod")]
        Willow = 11,
        [FishingRod("yew_rod")]
        Yew = 12,
        [FishingRod("butterfly_rod")]
        Butterfly = 13,
        [FishingRod("tranquility_rod")]
        Tranquility = 14,
        [FishingRod("lushang_rod")]
        LuShang = 15
    }

    public class FishingRodAttribute : Attribute
    {
        public string Resref { get; set; }

        public FishingRodAttribute(string resref)
        {
            Resref = resref;
        }
    }
}
