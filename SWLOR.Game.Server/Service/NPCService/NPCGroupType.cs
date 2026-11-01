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
        [NPCGroup("Great Arkanian Dragon")]
        AbandonedStation_GreatArkanianDragon = 61,
        [NPCGroup("Rooftop Sniper")]
        NarShaddaa_Sniper = 62,
        [NPCGroup("Slaver Captain")]
        NarShaddaa_SlaverCaptain = 63,
        [NPCGroup("Pirates")]
        NarShaddaa_Pirates = 64,
        [NPCGroup("Command Droid")]
        NarShaddaa_CommandDroid = 65,
        [NPCGroup("Ancient Worm")]
        Tatooine_AncientSandworm = 66,
        [NPCGroup("Red Vein Scavenger")]
        Viscara_RedVeinScavenger = 67,
        [NPCGroup("Pulse-Frame Training Droid")]
        Viscara_PulseFrameTrainingDroid = 68,
        [NPCGroup("Blood Frenzy Butcher")]
        Viscara_BloodFrenzyButcher = 69,
        [NPCGroup("Blood Frenzy Duelist")]
        Viscara_BloodFrenzyDuelist = 70,
        [NPCGroup("Blood Frenzy King")]
        Viscara_BloodFrenzyKing = 71,
        [NPCGroup("Holdout Recruits")]
        Viscara_Invincible_Adept = 72,
        [NPCGroup("Holdout Sergeants")]
        Viscara_Invincible_Specialist = 73,
        [NPCGroup("Holdout Lieutenant Sarn")]
        Viscara_Invincible_Warden = 74,
        [NPCGroup("Holdout Honor Guards")]
        Viscara_Invincible_InnerCircle = 75,
        [NPCGroup("Holdout Captain Haro Kel")]
        Viscara_Invincible_Master = 76,
        [NPCGroup("Holdout Recruits")]
        Viscara_VitalRupture_Adept = 77,
        [NPCGroup("Holdout Sergeants")]
        Viscara_VitalRupture_Specialist = 78,
        [NPCGroup("Holdout Lieutenant Korvan Tull")]
        Viscara_VitalRupture_Warden = 79,
        [NPCGroup("Holdout Honor Guards")]
        Viscara_VitalRupture_InnerCircle = 80,
        [NPCGroup("Holdout Captain Marek Dolan")]
        Viscara_VitalRupture_Master = 81,
        [NPCGroup("Holdout Recruits")]
        Viscara_SystemicShutdown_Adept = 82,
        [NPCGroup("Holdout Sergeants")]
        Viscara_SystemicShutdown_Specialist = 83,
        [NPCGroup("Holdout Lieutenant Vessik Rane")]
        Viscara_SystemicShutdown_Warden = 84,
        [NPCGroup("Holdout Honor Guards")]
        Viscara_SystemicShutdown_InnerCircle = 85,
        [NPCGroup("Holdout Captain Jorrik Vael")]
        Viscara_SystemicShutdown_Master = 86,
        [NPCGroup("Saber Storm Adepts")]
        Dantooine_SaberStorm_Adept = 87,
        [NPCGroup("Saber Storm Specialists")]
        Dantooine_SaberStorm_Specialist = 88,
        [NPCGroup("Saber Storm Warden")]
        Dantooine_SaberStorm_Warden = 89,
        [NPCGroup("Saber Storm Inner Circle")]
        Dantooine_SaberStorm_InnerCircle = 90,
        [NPCGroup("Saber Storm Master")]
        Dantooine_SaberStorm_Master = 91,
        [NPCGroup("Guardian Master Adepts")]
        Dantooine_GuardianMaster_Adept = 92,
        [NPCGroup("Guardian Master Specialists")]
        Dantooine_GuardianMaster_Specialist = 93,
        [NPCGroup("Guardian Master Warden")]
        Dantooine_GuardianMaster_Warden = 94,
        [NPCGroup("Guardian Master Inner Circle")]
        Dantooine_GuardianMaster_InnerCircle = 95,
        [NPCGroup("Guardian Master Paragon")]
        Dantooine_GuardianMaster_Paragon = 96,
        [NPCGroup("Saber Cyclone Adepts")]
        Dantooine_SaberCyclone_Adept = 97,
        [NPCGroup("Saber Cyclone Specialists")]
        Dantooine_SaberCyclone_Specialist = 98,
        [NPCGroup("Saber Cyclone Warden")]
        Dantooine_SaberCyclone_Warden = 99,
        [NPCGroup("Saber Cyclone Inner Circle")]
        Dantooine_SaberCyclone_InnerCircle = 100,
        [NPCGroup("Saber Cyclone Master")]
        Dantooine_SaberCyclone_Master = 101,
        [NPCGroup("Forgebound Initiates")]
        Tatooine_AbsoluteDefense_Adept = 102,
        [NPCGroup("Forgebound Journeymans")]
        Tatooine_AbsoluteDefense_Specialist = 103,
        [NPCGroup("Forge Warden Ssorrak")]
        Tatooine_AbsoluteDefense_Warden = 104,
        [NPCGroup("Forgebound Overseers")]
        Tatooine_AbsoluteDefense_InnerCircle = 105,
        [NPCGroup("Forge Master Kassrek")]
        Tatooine_AbsoluteDefense_Master = 106,
        [NPCGroup("Forgebound Initiates")]
        Tatooine_SoulAscension_Adept = 107,
        [NPCGroup("Forgebound Journeymans")]
        Tatooine_SoulAscension_Specialist = 108,
        [NPCGroup("Forge Warden Dael Karrow")]
        Tatooine_SoulAscension_Warden = 109,
        [NPCGroup("Forgebound Overseers")]
        Tatooine_SoulAscension_InnerCircle = 110,
        [NPCGroup("Forge Master Orsen Vale")]
        Tatooine_SoulAscension_Master = 111,
        [NPCGroup("Forgebound Initiates")]
        Tatooine_Forcebane_Adept = 112,
        [NPCGroup("Forgebound Journeymans")]
        Tatooine_Forcebane_Specialist = 113,
        [NPCGroup("Forge Warden Orbek Tal")]
        Tatooine_Forcebane_Warden = 114,
        [NPCGroup("Forgebound Overseers")]
        Tatooine_Forcebane_InnerCircle = 115,
        [NPCGroup("Forge Master Kovath Sull")]
        Tatooine_Forcebane_Master = 116,
        [NPCGroup("Stable Brawlers")]
        SmugglersMoon_CripplingDefense_Adept = 117,
        [NPCGroup("Stable Pit Fighters")]
        SmugglersMoon_CripplingDefense_Specialist = 118,
        [NPCGroup("Pit Boss Ssarok")]
        SmugglersMoon_CripplingDefense_Warden = 119,
        [NPCGroup("Stable Promoters")]
        SmugglersMoon_CripplingDefense_InnerCircle = 120,
        [NPCGroup("Stable Champion Thrask")]
        SmugglersMoon_CripplingDefense_Master = 121,
        [NPCGroup("Stable Brawlers")]
        SmugglersMoon_TempestBloom_Adept = 122,
        [NPCGroup("Stable Pit Fighters")]
        SmugglersMoon_TempestBloom_Specialist = 123,
        [NPCGroup("Pit Boss Dax Corvin")]
        SmugglersMoon_TempestBloom_Warden = 124,
        [NPCGroup("Stable Promoters")]
        SmugglersMoon_TempestBloom_InnerCircle = 125,
        [NPCGroup("Stable Champion Rolan Mace")]
        SmugglersMoon_TempestBloom_Master = 126,
        [NPCGroup("Stable Brawlers")]
        SmugglersMoon_RedBloom_Adept = 127,
        [NPCGroup("Stable Pit Fighters")]
        SmugglersMoon_RedBloom_Specialist = 128,
        [NPCGroup("Pit Boss Kessan Pru")]
        SmugglersMoon_RedBloom_Warden = 129,
        [NPCGroup("Stable Promoters")]
        SmugglersMoon_RedBloom_InnerCircle = 130,
        [NPCGroup("Stable Champion Ven Dolo")]
        SmugglersMoon_RedBloom_Master = 131,
        [NPCGroup("Wreck Crew Scrappers")]
        CZ220_AdamantineGuard_Adept = 132,
        [NPCGroup("Wreck Crew Riggers")]
        CZ220_AdamantineGuard_Specialist = 133,
        [NPCGroup("Crew Chief Rakkesh")]
        CZ220_AdamantineGuard_Warden = 134,
        [NPCGroup("Foreman's Hands")]
        CZ220_AdamantineGuard_InnerCircle = 135,
        [NPCGroup("Wreck Foreman Ssurgan")]
        CZ220_AdamantineGuard_Master = 136,
        [NPCGroup("Wreck Crew Scrappers")]
        CZ220_ScrapheapLockdown_Adept = 137,
        [NPCGroup("Wreck Crew Riggers")]
        CZ220_ScrapheapLockdown_Specialist = 138,
        [NPCGroup("Crew Chief Tobin Rask")]
        CZ220_ScrapheapLockdown_Warden = 139,
        [NPCGroup("Foreman's Hands")]
        CZ220_ScrapheapLockdown_InnerCircle = 140,
        [NPCGroup("Wreck Foreman Lucan Brandt")]
        CZ220_ScrapheapLockdown_Master = 141,
        [NPCGroup("Wreck Crew Scrappers")]
        CZ220_Worldbreaker_Adept = 142,
        [NPCGroup("Wreck Crew Riggers")]
        CZ220_Worldbreaker_Specialist = 143,
        [NPCGroup("Crew Chief Nalo Ven")]
        CZ220_Worldbreaker_Warden = 144,
        [NPCGroup("Foreman's Hands")]
        CZ220_Worldbreaker_InnerCircle = 145,
        [NPCGroup("Wreck Foreman Kaddo Rin")]
        CZ220_Worldbreaker_Master = 146,
        [NPCGroup("Canyon Drifters")]
        Tatooine_UnmovingCenter_Adept = 147,
        [NPCGroup("Canyon Sharpshooters")]
        Tatooine_UnmovingCenter_Specialist = 148,
        [NPCGroup("Range Warden Dorrim Vex")]
        Tatooine_UnmovingCenter_Warden = 149,
        [NPCGroup("High Table Guns")]
        Tatooine_UnmovingCenter_InnerCircle = 150,
        [NPCGroup("Range Marshal Tarek Kor")]
        Tatooine_UnmovingCenter_Master = 151,
        [NPCGroup("Canyon Drifters")]
        Tatooine_LastWord_Adept = 152,
        [NPCGroup("Canyon Sharpshooters")]
        Tatooine_LastWord_Specialist = 153,
        [NPCGroup("Range Warden Gruvo")]
        Tatooine_LastWord_Warden = 154,
        [NPCGroup("High Table Guns")]
        Tatooine_LastWord_InnerCircle = 155,
        [NPCGroup("Range Marshal Wuun Kalo")]
        Tatooine_LastWord_Master = 156,
        [NPCGroup("Canyon Drifters")]
        Tatooine_DeadMansHand_Adept = 157,
        [NPCGroup("Canyon Sharpshooters")]
        Tatooine_DeadMansHand_Specialist = 158,
        [NPCGroup("Range Warden Silas Crowe")]
        Tatooine_DeadMansHand_Warden = 159,
        [NPCGroup("High Table Guns")]
        Tatooine_DeadMansHand_InnerCircle = 160,
        [NPCGroup("Range Marshal Jonas Harlan")]
        Tatooine_DeadMansHand_Master = 161,
        [NPCGroup("Range Washouts")]
        SmugglersMoon_KillBox_Adept = 162,
        [NPCGroup("Range Instructors")]
        SmugglersMoon_KillBox_Specialist = 163,
        [NPCGroup("Range Overseer Sskeer")]
        SmugglersMoon_KillBox_Warden = 164,
        [NPCGroup("Black Cell Shooters")]
        SmugglersMoon_KillBox_InnerCircle = 165,
        [NPCGroup("Range Director Krassek")]
        SmugglersMoon_KillBox_Master = 166,
        [NPCGroup("Range Washouts")]
        SmugglersMoon_OneShot_Adept = 167,
        [NPCGroup("Range Instructors")]
        SmugglersMoon_OneShot_Specialist = 168,
        [NPCGroup("Range Overseer Callum Breck")]
        SmugglersMoon_OneShot_Warden = 169,
        [NPCGroup("Black Cell Shooters")]
        SmugglersMoon_OneShot_InnerCircle = 170,
        [NPCGroup("Range Director Zhessk")]
        SmugglersMoon_OneShot_Master = 171,
        [NPCGroup("Range Washouts")]
        SmugglersMoon_RainOfSteel_Adept = 172,
        [NPCGroup("Range Instructors")]
        SmugglersMoon_RainOfSteel_Specialist = 173,
        [NPCGroup("Range Overseer Perrin Aldous")]
        SmugglersMoon_RainOfSteel_Warden = 174,
        [NPCGroup("Black Cell Shooters")]
        SmugglersMoon_RainOfSteel_InnerCircle = 175,
        [NPCGroup("Range Director Daav Rhoun")]
        SmugglersMoon_RainOfSteel_Master = 176,
        [NPCGroup("Test Crew Technicians")]
        Hutlar_PerfectFlurry_Adept = 177,
        [NPCGroup("Test Crew Demolitionists")]
        Hutlar_PerfectFlurry_Specialist = 178,
        [NPCGroup("Test Supervisor Ketu Ran")]
        Hutlar_PerfectFlurry_Warden = 179,
        [NPCGroup("Site Chief's Officers")]
        Hutlar_PerfectFlurry_InnerCircle = 180,
        [NPCGroup("Site Chief Veshan Dru")]
        Hutlar_PerfectFlurry_Master = 181,
        [NPCGroup("Test Crew Technicians")]
        Hutlar_ThermalDetonator_Adept = 182,
        [NPCGroup("Test Crew Demolitionists")]
        Hutlar_ThermalDetonator_Specialist = 183,
        [NPCGroup("Test Supervisor Skorrik")]
        Hutlar_ThermalDetonator_Warden = 184,
        [NPCGroup("Site Chief's Officers")]
        Hutlar_ThermalDetonator_InnerCircle = 185,
        [NPCGroup("Site Chief Ssorgan")]
        Hutlar_ThermalDetonator_Master = 186,
        [NPCGroup("Test Crew Technicians")]
        Hutlar_OverloadBarrage_Adept = 187,
        [NPCGroup("Test Crew Demolitionists")]
        Hutlar_OverloadBarrage_Specialist = 188,
        [NPCGroup("Test Supervisor Hale Dorrin")]
        Hutlar_OverloadBarrage_Warden = 189,
        [NPCGroup("Site Chief's Officers")]
        Hutlar_OverloadBarrage_InnerCircle = 190,
        [NPCGroup("Site Chief Ansel Pryor")]
        Hutlar_OverloadBarrage_Master = 191,
        [NPCGroup("Last Stand of the Light Adepts")]
        Korriban_LastStandOfTheLight_Adept = 192,
        [NPCGroup("Last Stand of the Light Specialists")]
        Korriban_LastStandOfTheLight_Specialist = 193,
        [NPCGroup("Last Stand of the Light Warden")]
        Korriban_LastStandOfTheLight_Warden = 194,
        [NPCGroup("Last Stand of the Light Inner Circle")]
        Korriban_LastStandOfTheLight_InnerCircle = 195,
        [NPCGroup("Last Stand of the Light Master")]
        Korriban_LastStandOfTheLight_Master = 196,
        [NPCGroup("Hunger of the Dark Adepts")]
        Korriban_HungerOfTheDark_Adept = 197,
        [NPCGroup("Hunger of the Dark Specialists")]
        Korriban_HungerOfTheDark_Specialist = 198,
        [NPCGroup("Hunger of the Dark Warden")]
        Korriban_HungerOfTheDark_Warden = 199,
        [NPCGroup("Hunger of the Dark Inner Circle")]
        Korriban_HungerOfTheDark_InnerCircle = 200,
        [NPCGroup("Hunger of the Dark Master")]
        Korriban_HungerOfTheDark_Master = 201,
        [NPCGroup("Eclipse of Resolve Adepts")]
        Korriban_EclipseOfResolve_Adept = 202,
        [NPCGroup("Eclipse of Resolve Specialists")]
        Korriban_EclipseOfResolve_Specialist = 203,
        [NPCGroup("Eclipse of Resolve Warden")]
        Korriban_EclipseOfResolve_Warden = 204,
        [NPCGroup("Eclipse of Resolve Inner Circle")]
        Korriban_EclipseOfResolve_InnerCircle = 205,
        [NPCGroup("Eclipse of Resolve Master")]
        Korriban_EclipseOfResolve_Master = 206,
        [NPCGroup("Deserter Sappers")]
        Viscara_KillzoneBeacon_Adept = 207,
        [NPCGroup("Deserter Engineers")]
        Viscara_KillzoneBeacon_Specialist = 208,
        [NPCGroup("Deserter Sergeant Orgul")]
        Viscara_KillzoneBeacon_Warden = 209,
        [NPCGroup("Quartermaster's Guards")]
        Viscara_KillzoneBeacon_InnerCircle = 210,
        [NPCGroup("Deserter Commander Kuvaz")]
        Viscara_KillzoneBeacon_Master = 211,
        [NPCGroup("Deserter Sappers")]
        Viscara_EmergencyBunker_Adept = 212,
        [NPCGroup("Deserter Engineers")]
        Viscara_EmergencyBunker_Specialist = 213,
        [NPCGroup("Deserter Sergeant Bekko")]
        Viscara_EmergencyBunker_Warden = 214,
        [NPCGroup("Quartermaster's Guards")]
        Viscara_EmergencyBunker_InnerCircle = 215,
        [NPCGroup("Deserter Commander Mukk Dorl")]
        Viscara_EmergencyBunker_Master = 216,
        [NPCGroup("Deserter Sappers")]
        Viscara_DecisiveCommand_Adept = 217,
        [NPCGroup("Deserter Engineers")]
        Viscara_DecisiveCommand_Specialist = 218,
        [NPCGroup("Deserter Sergeant Grenn Vhon")]
        Viscara_DecisiveCommand_Warden = 219,
        [NPCGroup("Quartermaster's Guards")]
        Viscara_DecisiveCommand_InnerCircle = 220,
        [NPCGroup("Deserter Commander Halden Rhee")]
        Viscara_DecisiveCommand_Master = 221,
        [NPCGroup("Ward Orderlys")]
        MonCala_HoldTheLine_Adept = 222,
        [NPCGroup("Ward Guards")]
        MonCala_HoldTheLine_Specialist = 223,
        [NPCGroup("Ward Supervisor Aron Meel")]
        MonCala_HoldTheLine_Warden = 224,
        [NPCGroup("Matron's Attendants")]
        MonCala_HoldTheLine_InnerCircle = 225,
        [NPCGroup("Ward Chief Anchor")]
        MonCala_HoldTheLine_Master = 226,
        [NPCGroup("Ward Orderlys")]
        MonCala_EmergencyCocktail_Adept = 227,
        [NPCGroup("Ward Guards")]
        MonCala_EmergencyCocktail_Specialist = 228,
        [NPCGroup("Ward Supervisor Teno Barr")]
        MonCala_EmergencyCocktail_Warden = 229,
        [NPCGroup("Matron's Attendants")]
        MonCala_EmergencyCocktail_InnerCircle = 230,
        [NPCGroup("Ward Chief Koth Davin")]
        MonCala_EmergencyCocktail_Master = 231,
        [NPCGroup("Ward Orderlys")]
        MonCala_InfiniteConduit_Adept = 232,
        [NPCGroup("Ward Guards")]
        MonCala_InfiniteConduit_Specialist = 233,
        [NPCGroup("Ward Supervisor Dru Salin")]
        MonCala_InfiniteConduit_Warden = 234,
        [NPCGroup("Matron's Attendants")]
        MonCala_InfiniteConduit_InnerCircle = 235,
        [NPCGroup("Ward Chief Idris Voh")]
        MonCala_InfiniteConduit_Master = 236,
        [NPCGroup("Young Preserve Beasts")]
        Dathomir_ApexBite_Adept = 237,
        [NPCGroup("Pen-Scarred Preserve Beasts")]
        Dathomir_ApexBite_Specialist = 238,
        [NPCGroup("Breeder's Tukata")]
        Dathomir_ApexBite_Warden = 239,
        [NPCGroup("Elder Preserve Beasts")]
        Dathomir_ApexBite_InnerCircle = 240,
        [NPCGroup("Old Tukata")]
        Dathomir_ApexBite_Master = 241,
        [NPCGroup("Young Preserve Beasts")]
        Dathomir_UnbreakableBeast_Adept = 242,
        [NPCGroup("Pen-Scarred Preserve Beasts")]
        Dathomir_UnbreakableBeast_Specialist = 243,
        [NPCGroup("Breeder's Shyrack")]
        Dathomir_UnbreakableBeast_Warden = 244,
        [NPCGroup("Elder Preserve Beasts")]
        Dathomir_UnbreakableBeast_InnerCircle = 245,
        [NPCGroup("Old Shyrack")]
        Dathomir_UnbreakableBeast_Master = 246,
        [NPCGroup("Young Preserve Beasts")]
        Dathomir_AlphaRhythm_Adept = 247,
        [NPCGroup("Pen-Scarred Preserve Beasts")]
        Dathomir_AlphaRhythm_Specialist = 248,
        [NPCGroup("Breeder's Iriaz Bull")]
        Dathomir_AlphaRhythm_Warden = 249,
        [NPCGroup("Elder Preserve Beasts")]
        Dathomir_AlphaRhythm_InnerCircle = 250,
        [NPCGroup("Old Iriaz Bull")]
        Dathomir_AlphaRhythm_Master = 251,
        [NPCGroup("Young Grotto Beasts")]
        Dathomir_PrimalOverrun_Adept = 252,
        [NPCGroup("Goad-Scarred Grotto Beasts")]
        Dathomir_PrimalOverrun_Specialist = 253,
        [NPCGroup("Handler's Rancor")]
        Dathomir_PrimalOverrun_Warden = 254,
        [NPCGroup("Den Elder Grotto Beasts")]
        Dathomir_PrimalOverrun_InnerCircle = 255,
        [NPCGroup("Old Rancor")]
        Dathomir_PrimalOverrun_Master = 256,
        [NPCGroup("Young Grotto Beasts")]
        Dathomir_UntouchableInstinct_Adept = 257,
        [NPCGroup("Goad-Scarred Grotto Beasts")]
        Dathomir_UntouchableInstinct_Specialist = 258,
        [NPCGroup("Handler's Giant Brown Lizard")]
        Dathomir_UntouchableInstinct_Warden = 259,
        [NPCGroup("Den Elder Grotto Beasts")]
        Dathomir_UntouchableInstinct_InnerCircle = 260,
        [NPCGroup("Old Giant Brown Lizard")]
        Dathomir_UntouchableInstinct_Master = 261,
        [NPCGroup("Young Grotto Beasts")]
        Dathomir_ForceBondedBeast_Adept = 262,
        [NPCGroup("Goad-Scarred Grotto Beasts")]
        Dathomir_ForceBondedBeast_Specialist = 263,
        [NPCGroup("Handler's Wraid")]
        Dathomir_ForceBondedBeast_Warden = 264,
        [NPCGroup("Den Elder Grotto Beasts")]
        Dathomir_ForceBondedBeast_InnerCircle = 265,
        [NPCGroup("Old Wraid")]
        Dathomir_ForceBondedBeast_Master = 266,

        // Retain the historical groups above for persisted enum keys and migration.
        [NPCGroup("Trial Drill Droids")]
        Hutlar_StormDrillDroid = 267,
        [NPCGroup("Trial Duelist Droids")]
        Hutlar_StormDuelistDroid = 268,
        [NPCGroup("Trial Gatekeeper Droid")]
        Hutlar_StormGatekeeperDroid = 269,
        [NPCGroup("Trial Examiner Droids")]
        Hutlar_StormExaminerDroid = 270,
        [NPCGroup("Tempest Training Engine")]
        Hutlar_TempestTrainingEngine = 271,
        [NPCGroup("Trial Drill Droids")]
        Hutlar_GuardianPatrolDroid = 272,
        [NPCGroup("Trial Duelist Droids")]
        Hutlar_GuardianBulwarkDroid = 273,
        [NPCGroup("Trial Gatekeeper Droid")]
        Hutlar_GuardianGatekeeperDroid = 274,
        [NPCGroup("Trial Examiner Droids")]
        Hutlar_GuardianShieldDroid = 275,
        [NPCGroup("Bastion Training Engine")]
        Hutlar_BastionTrainingEngine = 276,
        [NPCGroup("Trial Drill Droids")]
        Hutlar_CycloneDrillDroid = 277,
        [NPCGroup("Trial Duelist Droids")]
        Hutlar_CycloneDuelistDroid = 278,
        [NPCGroup("Trial Gatekeeper Droid")]
        Hutlar_CycloneGatekeeperDroid = 279,
        [NPCGroup("Trial Examiner Droids")]
        Hutlar_CycloneExaminerDroid = 280,
        [NPCGroup("Vortex Training Engine")]
        Hutlar_VortexTrainingEngine = 281,
        [NPCGroup("Expedition Raiders")]
        MonCala_CryptRaider = 282,
        [NPCGroup("Expedition Marauders")]
        MonCala_CryptMarauder = 283,
        [NPCGroup("Expedition Captain Rhaan Tolek")]
        MonCala_CryptBreachCaptain = 284,
        [NPCGroup("Expedition Reavers")]
        MonCala_CryptReaver = 285,
        [NPCGroup("Expedition Leader Kaelen Dorr")]
        MonCala_CryptExpeditionLeader = 286,
        [NPCGroup("Expedition Raiders")]
        MonCala_VitaeReaverInitiate = 287,
        [NPCGroup("Expedition Marauders")]
        MonCala_VitaeReaverAdept = 288,
        [NPCGroup("Expedition Captain Zarrik Thane")]
        MonCala_VitaeReaverEnforcer = 289,
        [NPCGroup("Expedition Reavers")]
        MonCala_VitaeReaverBinder = 290,
        [NPCGroup("Expedition Leader Mordek Sull")]
        MonCala_VitaeReaverChief = 291,
        [NPCGroup("Expedition Raiders")]
        MonCala_UmbralRaiderScout = 292,
        [NPCGroup("Expedition Marauders")]
        MonCala_UmbralRaiderAdept = 293,
        [NPCGroup("Expedition Captain Tavik Orrun")]
        MonCala_UmbralRaiderCaptain = 294,
        [NPCGroup("Expedition Reavers")]
        MonCala_UmbralRaiderSaboteur = 295,
        [NPCGroup("Expedition Leader Lorrek Syn")]
        MonCala_UmbralRaiderCommander = 296,
        [NPCGroup("Foundry Droid")]
        Hutlar_FoundryDroid = 297,
        [NPCGroup("Station Horror")]
        AbandonedStation_Horror = 298,

        [NPCGroup("Eshan Dire Wolves")]
        Eshan_DireWolf = 309,
        [NPCGroup("Eshan Frost Dire Wolves")]
        Eshan_FrostWolf = 310,
        [NPCGroup("Eshan Dire Wolf Alpha")]
        Eshan_DireWolfAlpha = 299,
        [NPCGroup("Eshan Neo-Crusader Scouts")]
        Eshan_NeoCrusaderScout = 300,
        [NPCGroup("Eshan Neo-Crusader Hunters")]
        Eshan_NeoCrusaderHunter = 301,
        [NPCGroup("Eshan Neo-Crusader Vanguards")]
        Eshan_NeoCrusaderVanguard = 302,
        [NPCGroup("Eshan Neo-Crusader Heavy Gunners")]
        Eshan_NeoCrusaderHeavy = 303,
        [NPCGroup("Eshan Neo-Crusader Field Medics")]
        Eshan_NeoCrusaderMedic = 304,
        [NPCGroup("Eshan Neo-Crusader Captains")]
        Eshan_NeoCrusaderCaptain = 305,
        [NPCGroup("Eshan Thyrsian Exiles")]
        Eshan_SunGuard = 306,
        [NPCGroup("Eshan Scrapyard Smugglers")]
        Eshan_ScrapyardSmuggler = 307,
        [NPCGroup("Gorak Vesh, the Icebound")]
        Eshan_GorakVesh = 308,
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
