using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum NPCGroupType
    {
        [NPCGroup("Invalid")]
        Invalid = 0,
        [NPCGroup("Mynocks")]
        CZ220_Mynocks = 1,
        [NPCGroup("Malfunctioning Droids")]
        CZ220_MalfunctioningDroids = 2,
        [NPCGroup("Colicoid Experiment")]
        CZ220_ColicoidExperiment = 3,
        [NPCGroup("Kath Hounds")]
        Viscara_WildlandKathHounds = 4,
        [NPCGroup("Mandalorian Leader")]
        Viscara_MandalorianLeader = 5,
        [NPCGroup("Mandalorian Warriors")]
        Viscara_MandalorianWarriors = 6,
        [NPCGroup("Mandalorian Rangers")]
        Viscara_MandalorianRangers = 7,
        [NPCGroup("Outlaws")]
        Viscara_WildwoodsOutlaws = 8,
        [NPCGroup("Gimpassas")]
        Viscara_WildwoodsGimpassas = 9,
        [NPCGroup("Kinraths")]
        Viscara_WildwoodsKinraths = 10,
        [NPCGroup("Cairnmogs")]
        Viscara_ValleyCairnmogs = 11,
        [NPCGroup("Fleshleader")]
        Viscara_VellenFleshleader = 12,
        [NPCGroup("Raivors")]
        Viscara_DeepMountainRaivors = 13,
        [NPCGroup("Warocas")]
        Viscara_WildlandsWarocas = 14,
        [NPCGroup("Nashtah")]
        Viscara_ValleyNashtah = 15,
        [NPCGroup("Crystal Spider")]
        Viscara_CrystalSpider = 16,
        [NPCGroup("Aradile")]
        MonCala_Aradile = 17,
        [NPCGroup("Viper")]
        MonCala_Viper = 18,
        [NPCGroup("Amphi-Hydrus")]
        MonCala_AmphiHydrus = 19,
        [NPCGroup("Eco Terrorist")]
        MonCala_EcoTerrorist = 20,
        [NPCGroup("Flesheater")]
        Viscara_VellenFlesheater = 21,
        [NPCGroup("Zombie Rancor")]
        AbandonedStation_Boss = 22,
        [NPCGroup("Womprat")]
        Tatooine_Womprat = 23,
        [NPCGroup("Sandswimmer")]
        Tatooine_Sandswimmer = 24,
        [NPCGroup("Sand Beetle")]
        Tatooine_Wraid = 25,
        [NPCGroup("Sand Demon")]
        Tatooine_SandDemon = 26,
        [NPCGroup("Tusken Raider")]
        Tatooine_TuskenRaider = 27,
        [NPCGroup("Byysk")]
        Hutlar_Byysk = 28,
        [NPCGroup("Qion Slugs")]
        Hutlar_QionSlugs = 29,
        [NPCGroup("Qion Tigers")]
        Hutlar_QionTigers = 30
    }

    public class NPCGroupAttribute : Attribute
    {
        public string Name { get; set; }

        public NPCGroupAttribute(string name)
        {
            Name = name;
        }
    }
}
