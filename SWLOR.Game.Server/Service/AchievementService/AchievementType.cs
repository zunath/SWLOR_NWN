using System;

namespace SWLOR.Game.Server.Service.AchievementService
{
    public enum AchievementType
    {
        [Achievement("Invalid", "Invalid", false)]
        Invalid = 0,
        [Achievement("Kill Enemies I", "Kill 10 enemies.", true)]
        KillEnemies1 = 1,
        [Achievement("Kill Enemies II", "Kill 50 enemies.", true)]
        KillEnemies2 = 2,
        [Achievement("Kill Enemies III", "Kill 500 enemies.", true)]
        KillEnemies3 = 3,
        [Achievement("Kill Enemies IV", "Kill 2,000 enemies.", true)]
        KillEnemies4 = 4,
        [Achievement("Kill Enemies V", "Kill 10,000 enemies.", true)]
        KillEnemies5 = 5,
        [Achievement("Kill Enemies VI", "Kill 100,000 enemies.", true)]
        KillEnemies6 = 6,
        [Achievement("Learn Perks I", "Learn 1 Perk", true)]
        LearnPerks1 = 7,
        [Achievement("Learn Perks II", "Learn 20 Perks", true)]
        LearnPerks2 = 8,
        [Achievement("Learn Perks III", "Learn 50 Perks", true)]
        LearnPerks3 = 9,
        [Achievement("Learn Perks IV", "Learn 100 Perks", true)]
        LearnPerks4 = 10,
        [Achievement("Learn Perks V", "Learn 500 Perks", true)]
        LearnPerks5 = 11,
        [Achievement("Gain Skill Points I", "Gain 1 Skill Point", true)]
        GainSkills1 = 12,
        [Achievement("Gain Skill Points II", "Gain 50 Skill Points", true)]
        GainSkills2 = 13,
        [Achievement("Gain Skill Points III", "Gain 150 Skill Points", true)]
        GainSkills3 = 14,
        [Achievement("Gain Skill Points IV", "Gain 250 Skill Points", true)]
        GainSkills4 = 15,
        [Achievement("Gain Skill Points V", "Gain 500 Skill Points", true)]
        GainSkills5 = 16,
        [Achievement("Gain Skill Points VI", "Gain 1000 Skill Points", true)]
        GainSkills6 = 17,
        [Achievement("Complete Quests I", "Complete 1 Quest", true)]
        CompleteQuests1 = 18,
        [Achievement("Complete Quests II", "Complete 10 Quests", true)]
        CompleteQuests2 = 19,
        [Achievement("Complete Quests III", "Complete 50 Quests", true)]
        CompleteQuests3 = 20,
        [Achievement("Complete Quests IV", "Complete 100 Quests", true)]
        CompleteQuests4 = 21,
        [Achievement("Complete Quests V", "Complete 500 Quests", true)]
        CompleteQuests5 = 22,
        [Achievement("Complete Quests VI", "Complete 1000 Quests", true)]
        CompleteQuests6 = 23,
        [Achievement("Complete Quests VII", "Complete 1500 Quests", true)]
        CompleteQuests7 = 24,
        [Achievement("Complete Quests VIII", "Complete 2000 Quests", true)]
        CompleteQuests8 = 25,
        [Achievement("Complete Quests IX", "Complete 3500 Quests", true)]
        CompleteQuests9 = 26,
        [Achievement("Complete Quests X", "Complete 5000 Quests", true)]
        CompleteQuests10 = 27,
        [Achievement("Craft Items I", "Craft 1 Item", true)]
        CraftItems1 = 28,
        [Achievement("Craft Items II", "Craft 10 Items", true)]
        CraftItems2 = 29,
        [Achievement("Craft Items III", "Craft 50 Items", true)]
        CraftItems3 = 30,
        [Achievement("Craft Items IV", "Craft 100 Items", true)]
        CraftItems4 = 31,
        [Achievement("Craft Items V", "Craft 500 Items", true)]
        CraftItems5 = 32,
        [Achievement("Craft Items VI", "Craft 1000 Items", true)]
        CraftItems6 = 33,
        [Achievement("Craft Items VII", "Craft 1500 Items", true)]
        CraftItems7 = 34,
        [Achievement("Craft Items VIII", "Craft 2000 Items", true)]
        CraftItems8 = 35,
        [Achievement("Craft Items IX", "Craft 3500 Items", true)]
        CraftItems9 = 36,
        [Achievement("Craft Items X", "Craft 5000 Items", true)]
        CraftItems10 = 37,

        [Achievement("Explore Abandoned Station", "Explore the Abandoned Station", true)]
        ExploreAbandonedStation = 38,

        [Achievement("Explore CZ-220", "Explore CZ-220", true)]
        ExploreCZ220 = 39,

        [Achievement("Explore Qion Tundra", "Explore the Qion Tundra on Hutlar", true)]
        ExploreHutlarQionTundra = 40,

        [Achievement("Explore Qion Valley", "Explore the Qion Valley on Hutlar", true)]
        ExploreQionValley = 41,

        [Achievement("Explore Abandoned Outpost", "Explore the Abandoned Outpost on Hutlar", true)]
        ExploreHutlarAbandonedOutpost = 42,

        [Achievement("Explore Hutlar Outpost", "Explore the Hutlar Outpost", true)]
        ExploreHutlarOutpost = 43,

        [Achievement("Explore Dac City", "Explore Dac City on Mon Cala", true)]
        ExploreMonCalaDacCity = 44,

        [Achievement("Explore Coral Isles Inner", "Explore the Inner Coral Isles on Mon Cala", true)]
        ExploreMonCalaCoralIslesInner = 45,

        [Achievement("Explore Coral Isles Outer", "Explore the Outer Coral Isles on Mon Cala", true)]
        ExploreMonCalaCoralIslesOuter = 46,

        [Achievement("Explore Coral Isles Facility", "Explore the Facility on Mon Cala", true)]
        ExploreMonCalaFacility = 47,

        [Achievement("Explore Player Bank", "Explore a player's bank", true)]
        ExplorePlayerBank = 48,

        [Achievement("Explore Player Cantina", "Explore a player's cantina", true)]
        ExplorePlayerCantina = 49,

        [Achievement("Explore Player City Hall", "Explore a player's city hall", true)]
        ExplorePlayerCityHall = 50,

        [Achievement("Explore Player Medical Center", "Explore a player's medical center", true)]
        ExplorePlayerMedicalCenter = 51,

        [Achievement("Explore Player Starport", "Explore a player's starport", true)]
        ExplorePlayerStarport = 52,

        [Achievement("Explore Hutlar Orbit", "Explore Hutlar's Orbit", true)]
        ExploreHutlarOrbit = 53,

        [Achievement("Explore Mon Cala Orbit", "Explore Mon Cala's Orbit", true)]
        ExploreMonCalaOrbit = 54,

        [Achievement("Explore Tatooine Orbit", "Explore Tatooine's Orbit", true)]
        ExploreTatooineOrbit = 55,

        [Achievement("Explore Viscara Orbit", "Explore Viscara's Orbit", true)]
        ExploreViscaraOrbit = 56,

        [Achievement("Explore Consular Starship", "Explore a Consular class starship", true)]
        ExploreStarshipConsular = 57,

        [Achievement("Explore Falchion Starship", "Explore a Falchion class starship", true)]
        ExploreStarshipFalchion = 58,

        [Achievement("Explore Hound Starship", "Explore a Hound class starship", true)]
        ExploreStarshipHound = 59,

        [Achievement("Explore Merchant Starship", "Explore a Merchant class starship", true)]
        ExploreStarshipMerchant = 60,

        [Achievement("Explore Mule Starship", "Explore a Mule class starship", true)]
        ExploreStarshipMule = 61,

        [Achievement("Explore Panther Starship", "Explore a Panther class starship", true)]
        ExploreStarshipPanther = 62,

        [Achievement("Explore Saber Starship", "Explore a Saber class starship", true)]
        ExploreStarshipSaber = 63,

        [Achievement("Explore Striker Starship", "Explore a Striker class starship", true)]
        ExploreStarshipStriker = 64,

        [Achievement("Explore Throne Starship", "Explore a Throne class starship", true)]
        ExploreStarshipThrone = 65,

        [Achievement("Explore Anchorhead", "Explore Anchorhead on Tatooine", true)]
        ExploreTatooineAnchorhead = 66,

        [Achievement("Explore Tatooine Desert", "Explore the desert on Tatooine", true)]
        ExploreTatooineDesert = 67,

        [Achievement("Explore Deep Canyon", "Explore the deep canyon on Tatooine", true)]
        ExploreTatooineDeepCanyon = 68,

        [Achievement("Explore Mos Esper", "Explore Mos Esper on Tatooine", true)]
        ExploreTatooineMosEsper = 69,

        [Achievement("Explore Tatooine Dunes", "Explore the dunes on Tatooine", true)]
        ExploreTatooineDunes = 70,

        [Achievement("Explore Tusken Camp", "Explore the Tusken raider camp on Tatooine", true)]
        ExploreTatooineTuskenRaiderCamp = 71,

        [Achievement("Explore Tusken Cave", "Explore the Tusken cave on Tatooine", true)]
        ExploreTatooineTuskenCave = 72,

        [Achievement("Explore Viscara Cavern", "Explore the cavern on Viscara", true)]
        ExploreViscaraCavern = 73,

        [Achievement("Explore Coxxion Base", "Explore the Coxxion Base on Viscara", true)]
        ExploreViscaraCoxxionBase = 74,

        [Achievement("Explore Coxxion HQ", "Explore the Coxxion Headquarters on Viscara", true)]
        ExploreViscaraCoxxionHeadquarters = 75,

        [Achievement("Explore Deep Mountains", "Explore the Deep Mountains on Viscara", true)]
        ExploreViscaraDeepMountains = 76,

        [Achievement("Explore Deepwoods", "Explore the Deepwoods on Viscara", true)]
        ExploreViscaraDeepwoods = 77,

        [Achievement("Explore Eastern Swamplands", "Explore the Eastern Swamplands on Viscara", true)]
        ExploreViscaraEasternSwamplands = 78,

        [Achievement("Explore Viscara Lake", "Explore the Lake on Viscara", true)]
        ExploreViscaraLake = 79,

        [Achievement("Explore Mandalorian Facility", "Explore the Mandalorian Facility on Viscara", true)]
        ExploreViscaraMandalorianFacility = 80,

        [Achievement("Explore Mountain Valley", "Explore the Mountain Valley on Viscara", true)]
        ExploreViscaraMountainValley = 81,

        [Achievement("Explore Racin' Jims", "Explore Racin' Jims Cantina on Viscara", true)]
        ExploreViscaraRacinJims = 82,

        [Achievement("Explore Republic Base", "Explore the Republic Base on Viscara", true)]
        ExploreViscaraRepublicBase = 83,

        [Achievement("Explore Sith Lake Outpost", "Explore the Sith Lake Outpost on Viscara", true)]
        ExploreViscaraSithLakeOutpost = 84,

        [Achievement("Explore Veles Colony", "Explore Veles Colony on Viscara", true)]
        ExploreViscaraVelesColony = 85,

        [Achievement("Explore Western Swamplands", "Explore the Western Swamplands on Viscara", true)]
        ExploreViscaraWesternSwamplands = 86,

        [Achievement("Explore Wildlands", "Explore the Wildlands on Viscara", true)]
        ExploreViscaraWildlands = 87,

        [Achievement("Explore Wildwoods", "Explore the Wildwoods on Viscara", true)]
        ExploreViscaraWildwoods = 88,

        [Achievement("Explore Rocky Pass", "Explore the Rocky Pass on Tatooine", true)]
        ExploreTatooineRockyPass = 89,

        [Achievement("Explore Player Apartment", "Explore a player's apartment", true)]
        ExplorePlayerApartment = 90,

        [Achievement("Explore Player House", "Explore a player's house", true)]
        ExplorePlayerHouse = 91,

        [Achievement("Explore Korriban Orbit", "Explore Korriban's Orbit", true)]
        ExploreKorribanOrbit = 92,

        [Achievement("Explore Wastelands", "Explore the wastelands on Korriban", true)]
        ExploreKorribanWastelands = 93,

        [Achievement("Explore Sith Crypt", "Explore the Sith crypt on Korriban", true)]
        ExploreKorribanSithCrypt = 94,

        [Achievement("Explore Korriban Caverns", "Explore the caverns on Korriban", true)]
        ExploreKorribanCaverns = 95,

        [Achievement("Explore Sunkenhead Swamps", "Explore the Sunkenhead Swamps on Mon Cala.", true)]
        ExploreMonCalaSunkenhedgeSwamp = 96,

        [Achievement("Explore Sharptooth Jungle", "Explore the Sharptooth Jungle on Mon Cala", true)]
        ExploreMonCalaSharptoothJungle = 97,

        [Achievement("Explore Sharptooth Caverns", "Explore the Sharptooth Caverns on Mon Cala", true)]
        ExploreMonCalaSharptoothCaverns = 98,

        [Achievement("Explore Qion Foothills", "Explore the Qion Foothills on Hutlar.", true)]
        ExploreHutlarQionFoothills = 99,

        [Achievement("Explore Dathomir Orbit", "Explore Dathomir's Orbit", true)]
        ExploreDathomirOrbit = 100,

        [Achievement("Explore Dathomir Cave Ruins", "Explore Dathomir Cave Ruins", true)]
        ExploreDathomirCaveRuins = 101,

        [Achievement("Explore Dathomir Desert", "Explore Dathomir Desert", true)]
        ExploreDathomirDesert = 102,

        [Achievement("Explore Dathomir Grotto Caverns", "Explore Dathomir Grotto Caverns", true)]
        ExploreDathomirGrottoCaverns = 103,

        [Achievement("Explore Dathomir Grottos", "Explore Dathomir Grottos", true)]
        ExploreDathomirGrottos = 104,

        [Achievement("Explore Dathomir Jungles", "Explore Dathomir Jungles", true)]
        ExploreDathomirJungles = 105,

        [Achievement("Explore Dathomir Mountain Caves", "Explore Dathomir Mountain Caves", true)]
        ExploreDathomirMountainCaves = 106,

        [Achievement("Explore Dathomir Mountains", "Explore Dathomir Mountains", true)]
        ExploreDathomirMountains = 107,

        [Achievement("Explore Dathomir Ruin Base", "Explore Dathomir Ruin Base", true)]
        ExploreDathomirRuinBase = 108,

        [Achievement("Explore Dathomir Tribe Village", "Explore Dathomir Tribe Village", true)]
        ExploreDathomirTribeVillage = 109,

        [Achievement("The Legendary Rod", "Completed the quest 'The Legendary Rod'.", true)]
        TheLegendaryRod = 110,

        [Achievement("Explore Dantooine Orbit", "Explore Dantooine's Orbit.", true)]
        ExploreDantooineOrbit = 111,

        [Achievement("Explore Dantooine Fields", "Explore Dantooine's Fields.", true)]
        ExploreDantooineFields = 112,

        [Achievement("Explore Dantooine Colony", "Explore Dantooine's Colony.", true)]
        ExploreDantooineColony = 113,

        [Achievement("Explore Dantooine Lake", "Explore Dantooine's Lake.", true)]
        ExploreDantooineLake = 114,
        
        [Achievement("Explore Dantooine Kinrath Tunnels", "Explore Dantooine's Kinrath Tunnels.", true)]
        ExploreDantooineKinrathTunnels = 115,

        [Achievement("Explore Dantooine Wild Plains", "Explore Dantooine's Wild Plains.", true)]
        ExploreDantooineWildPlains = 116,

        [Achievement("Explore Dantooine Tribe", "Explore Dantooine's Tribe.", true)]
        ExploreDantooineTribe = 117,

        [Achievement("Explore Dantooine Abandoned Warehouse", "Explore Dantooine's Abandoned Base.", true)]
        ExploreDantooineAbandonedWarehouse = 118,

        [Achievement("Explore Dantooine Jungle Mountain ", "Explore Dantooine's Jungle Mountains.", true)]
        ExploreDantooineJungleMountain = 119,
    }

    public class AchievementAttribute: Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public AchievementAttribute(string name, string description, bool isActive)
        {
            Name = name;
            Description = description;
            IsActive = isActive;
        }
    }
}
