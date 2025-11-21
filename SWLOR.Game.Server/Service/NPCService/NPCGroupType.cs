using System;
using System.Diagnostics.CodeAnalysis;

namespace SWLOR.Game.Server.Service.NPCService
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
        [NPCGroup("Gimpassa Hatchlings")]
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
        Tatooine_SandBeetle = 25,
        [NPCGroup("Sand Demon")]
        Tatooine_SandDemon = 26,
        [NPCGroup("Tusken Raider")]
        Tatooine_TuskenRaider = 27,
        [NPCGroup("Byysk")]
        Hutlar_Byysk = 28,
        [NPCGroup("Qion Slugs")]
        Hutlar_QionSlugs = 29,
        [NPCGroup("Qion Tigers")]
        Hutlar_QionTigers = 30,
        [NPCGroup("Pelko Bug Swarm")]
        Korriban_Tukata = 31,
        [NPCGroup("K'lor'slug")]
        Korriban_Hssiss = 32,
        [NPCGroup("Shyrack")]
        Korriban_Shyrack = 33,
        [NPCGroup("Moraband Serpent")]
        Korriban_MorabandSerpent = 34,
        [NPCGroup("Sith Apprentice")]
        Korriban_SithApprenticeGhost = 35,
        [NPCGroup("Wraid")]
        Korriban_Terentatek = 36,
        [NPCGroup("Octotench")]
        MonCala_Octotench = 37,
        [NPCGroup("Microtench")]
        MonCala_Microtench = 38,
        [NPCGroup("Scorchellus")]
        MonCala_Scorchellus = 39,

        [NPCGroup("Chirodactyl")]
        Dathomir_Chirodactyl = 40,
        [NPCGroup("Dragon Turtle")]
        Dathomir_DragonTurtle = 41,
        [NPCGroup("Kwi Guardian")]
        Dathomir_KwiGuardian = 42,
        [NPCGroup("Kwi Shaman")]
        Dathomir_KwiShaman = 43,
        [NPCGroup("Kwi Tribal")]
        Dathomir_KwiTribal = 44,
        [NPCGroup("Purbole")]
        Dathomir_Purbole = 45,
        [NPCGroup("Shear Mite")]
        Dathomir_ShearMite = 46,
        [NPCGroup("Sprantal")]
        Dathomir_Sprantal = 47,
        [NPCGroup("Squellbug")]
        Dathomir_Squellbug = 48,
        [NPCGroup("Ssurian")]
        Dathomir_Ssurian = 49,
        [NPCGroup("Swampland Bug")]
        Dathomir_SwamplandBug = 50,
        [NPCGroup("Kinrath Queen")]
        Dantooine_KinrathQueen = 51,
        [NPCGroup("Iriaz")]
        Dantooine_Iriaz = 52,
        [NPCGroup("Voritor Lizard")]
        Dantooine_VoritorLizard = 53,
        [NPCGroup("Gizka")]
        Dantooine_Gizka = 54,
        [NPCGroup("Plains Thune")]
        Dantooine_PlainsThune = 55,
        [NPCGroup("Bol")]
        Dantooine_Bol = 56,
        [NPCGroup("Byysk Guardian")]
        Byysk_Guardian = 57,
        [NPCGroup("Korriban Initiates")]
        Korriban_RogueInitiates = 58,
        [NPCGroup("Korriban Frog")]
        Korriban_AlchemizedFrog = 59,
        [NPCGroup("Dantari Shaman")]
        Dantooine_DantariShaman = 60,

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
