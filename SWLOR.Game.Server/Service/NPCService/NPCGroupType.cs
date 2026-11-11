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
        Viscara_HoldoutRecruits = 72,
        [NPCGroup("Holdout Sergeants")]
        Viscara_HoldoutSergeants = 73,
        [NPCGroup("Holdout Lieutenant Sarn")]
        Viscara_HoldoutLieutenantSarn = 74,
        [NPCGroup("Holdout Honor Guards")]
        Viscara_HoldoutHonorGuards = 75,
        [NPCGroup("Holdout Captain Haro Kel")]
        Viscara_HoldoutCaptainHaroKel = 76,
        [NPCGroup("Holdout Lieutenant Korvan Tull")]
        Viscara_HoldoutLieutenantKorvanTull = 79,
        [NPCGroup("Holdout Captain Marek Dolan")]
        Viscara_HoldoutCaptainMarekDolan = 81,
        [NPCGroup("Holdout Lieutenant Vessik Rane")]
        Viscara_HoldoutLieutenantVessikRane = 84,
        [NPCGroup("Holdout Captain Jorrik Vael")]
        Viscara_HoldoutCaptainJorrikVael = 86,
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
        Tatooine_ForgeboundInitiates = 102,
        [NPCGroup("Forgebound Journeymans")]
        Tatooine_ForgeboundJourneymans = 103,
        [NPCGroup("Forge Warden Ssorrak")]
        Tatooine_ForgeWardenSsorrak = 104,
        [NPCGroup("Forgebound Overseers")]
        Tatooine_ForgeboundOverseers = 105,
        [NPCGroup("Forge Master Kassrek")]
        Tatooine_ForgeMasterKassrek = 106,
        [NPCGroup("Forge Warden Dael Karrow")]
        Tatooine_ForgeWardenDaelKarrow = 109,
        [NPCGroup("Forge Master Orsen Vale")]
        Tatooine_ForgeMasterOrsenVale = 111,
        [NPCGroup("Forge Warden Orbek Tal")]
        Tatooine_ForgeWardenOrbekTal = 114,
        [NPCGroup("Forge Master Kovath Sull")]
        Tatooine_ForgeMasterKovathSull = 116,
        [NPCGroup("Stable Brawlers")]
        SmugglersMoon_StableBrawlers = 117,
        [NPCGroup("Stable Pit Fighters")]
        SmugglersMoon_StablePitFighters = 118,
        [NPCGroup("Pit Boss Ssarok")]
        SmugglersMoon_PitBossSsarok = 119,
        [NPCGroup("Stable Promoters")]
        SmugglersMoon_StablePromoters = 120,
        [NPCGroup("Stable Champion Thrask")]
        SmugglersMoon_StableChampionThrask = 121,
        [NPCGroup("Pit Boss Dax Corvin")]
        SmugglersMoon_PitBossDaxCorvin = 124,
        [NPCGroup("Stable Champion Rolan Mace")]
        SmugglersMoon_StableChampionRolanMace = 126,
        [NPCGroup("Pit Boss Kessan Pru")]
        SmugglersMoon_PitBossKessanPru = 129,
        [NPCGroup("Stable Champion Ven Dolo")]
        SmugglersMoon_StableChampionVenDolo = 131,
        [NPCGroup("Wreck Crew Scrappers")]
        CZ220_WreckCrewScrappers = 132,
        [NPCGroup("Wreck Crew Riggers")]
        CZ220_WreckCrewRiggers = 133,
        [NPCGroup("Crew Chief Rakkesh")]
        CZ220_CrewChiefRakkesh = 134,
        [NPCGroup("Foreman's Hands")]
        CZ220_ForemansHands = 135,
        [NPCGroup("Wreck Foreman Ssurgan")]
        CZ220_WreckForemanSsurgan = 136,
        [NPCGroup("Crew Chief Tobin Rask")]
        CZ220_CrewChiefTobinRask = 139,
        [NPCGroup("Wreck Foreman Lucan Brandt")]
        CZ220_WreckForemanLucanBrandt = 141,
        [NPCGroup("Crew Chief Nalo Ven")]
        CZ220_CrewChiefNaloVen = 144,
        [NPCGroup("Wreck Foreman Kaddo Rin")]
        CZ220_WreckForemanKaddoRin = 146,
        [NPCGroup("Canyon Drifters")]
        Tatooine_CanyonDrifters = 147,
        [NPCGroup("Canyon Sharpshooters")]
        Tatooine_CanyonSharpshooters = 148,
        [NPCGroup("Range Warden Dorrim Vex")]
        Tatooine_RangeWardenDorrimVex = 149,
        [NPCGroup("High Table Guns")]
        Tatooine_HighTableGuns = 150,
        [NPCGroup("Range Marshal Tarek Kor")]
        Tatooine_RangeMarshalTarekKor = 151,
        [NPCGroup("Range Warden Gruvo")]
        Tatooine_RangeWardenGruvo = 154,
        [NPCGroup("Range Marshal Wuun Kalo")]
        Tatooine_RangeMarshalWuunKalo = 156,
        [NPCGroup("Range Warden Silas Crowe")]
        Tatooine_RangeWardenSilasCrowe = 159,
        [NPCGroup("Range Marshal Jonas Harlan")]
        Tatooine_RangeMarshalJonasHarlan = 161,
        [NPCGroup("Range Washouts")]
        SmugglersMoon_RangeWashouts = 162,
        [NPCGroup("Range Instructors")]
        SmugglersMoon_RangeInstructors = 163,
        [NPCGroup("Range Overseer Sskeer")]
        SmugglersMoon_RangeOverseerSskeer = 164,
        [NPCGroup("Black Cell Shooters")]
        SmugglersMoon_BlackCellShooters = 165,
        [NPCGroup("Range Director Krassek")]
        SmugglersMoon_RangeDirectorKrassek = 166,
        [NPCGroup("Range Overseer Callum Breck")]
        SmugglersMoon_RangeOverseerCallumBreck = 169,
        [NPCGroup("Range Director Zhessk")]
        SmugglersMoon_RangeDirectorZhessk = 171,
        [NPCGroup("Range Overseer Perrin Aldous")]
        SmugglersMoon_RangeOverseerPerrinAldous = 174,
        [NPCGroup("Range Director Daav Rhoun")]
        SmugglersMoon_RangeDirectorDaavRhoun = 176,
        [NPCGroup("Test Crew Technicians")]
        Hutlar_TestCrewTechnicians = 177,
        [NPCGroup("Test Crew Demolitionists")]
        Hutlar_TestCrewDemolitionists = 178,
        [NPCGroup("Test Supervisor Ketu Ran")]
        Hutlar_TestSupervisorKetuRan = 179,
        [NPCGroup("Site Chief's Officers")]
        Hutlar_SiteChiefsOfficers = 180,
        [NPCGroup("Site Chief Veshan Dru")]
        Hutlar_SiteChiefVeshanDru = 181,
        [NPCGroup("Test Supervisor Skorrik")]
        Hutlar_TestSupervisorSkorrik = 184,
        [NPCGroup("Site Chief Ssorgan")]
        Hutlar_SiteChiefSsorgan = 186,
        [NPCGroup("Test Supervisor Hale Dorrin")]
        Hutlar_TestSupervisorHaleDorrin = 189,
        [NPCGroup("Site Chief Ansel Pryor")]
        Hutlar_SiteChiefAnselPryor = 191,
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
        Viscara_DeserterSappers = 207,
        [NPCGroup("Deserter Engineers")]
        Viscara_DeserterEngineers = 208,
        [NPCGroup("Deserter Sergeant Orgul")]
        Viscara_DeserterSergeantOrgul = 209,
        [NPCGroup("Quartermaster's Guards")]
        Viscara_QuartermastersGuards = 210,
        [NPCGroup("Deserter Commander Kuvaz")]
        Viscara_DeserterCommanderKuvaz = 211,
        [NPCGroup("Deserter Sergeant Bekko")]
        Viscara_DeserterSergeantBekko = 214,
        [NPCGroup("Deserter Commander Mukk Dorl")]
        Viscara_DeserterCommanderMukkDorl = 216,
        [NPCGroup("Deserter Sergeant Grenn Vhon")]
        Viscara_DeserterSergeantGrennVhon = 219,
        [NPCGroup("Deserter Commander Halden Rhee")]
        Viscara_DeserterCommanderHaldenRhee = 221,
        [NPCGroup("Ward Orderlys")]
        MonCala_WardOrderlys = 222,
        [NPCGroup("Ward Guards")]
        MonCala_WardGuards = 223,
        [NPCGroup("Ward Supervisor Aron Meel")]
        MonCala_WardSupervisorAronMeel = 224,
        [NPCGroup("Matron's Attendants")]
        MonCala_MatronsAttendants = 225,
        [NPCGroup("Ward Chief Anchor")]
        MonCala_WardChiefAnchor = 226,
        [NPCGroup("Ward Supervisor Teno Barr")]
        MonCala_WardSupervisorTenoBarr = 229,
        [NPCGroup("Ward Chief Koth Davin")]
        MonCala_WardChiefKothDavin = 231,
        [NPCGroup("Ward Supervisor Dru Salin")]
        MonCala_WardSupervisorDruSalin = 234,
        [NPCGroup("Ward Chief Idris Voh")]
        MonCala_WardChiefIdrisVoh = 236,
        [NPCGroup("Young Preserve Beasts")]
        Dathomir_YoungPreserveBeasts = 237,
        [NPCGroup("Pen-Scarred Preserve Beasts")]
        Dathomir_PenScarredPreserveBeasts = 238,
        [NPCGroup("Breeder's Tukata")]
        Dathomir_BreedersTukata = 239,
        [NPCGroup("Elder Preserve Beasts")]
        Dathomir_ElderPreserveBeasts = 240,
        [NPCGroup("Old Tukata")]
        Dathomir_OldTukata = 241,
        [NPCGroup("Breeder's Shyrack")]
        Dathomir_BreedersShyrack = 244,
        [NPCGroup("Old Shyrack")]
        Dathomir_OldShyrack = 246,
        [NPCGroup("Breeder's Iriaz Bull")]
        Dathomir_BreedersIriazBull = 249,
        [NPCGroup("Old Iriaz Bull")]
        Dathomir_OldIriazBull = 251,
        [NPCGroup("Young Grotto Beasts")]
        Dathomir_YoungGrottoBeasts = 252,
        [NPCGroup("Goad-Scarred Grotto Beasts")]
        Dathomir_GoadScarredGrottoBeasts = 253,
        [NPCGroup("Handler's Rancor")]
        Dathomir_HandlersRancor = 254,
        [NPCGroup("Den Elder Grotto Beasts")]
        Dathomir_DenElderGrottoBeasts = 255,
        [NPCGroup("Old Rancor")]
        Dathomir_OldRancor = 256,
        [NPCGroup("Handler's Giant Brown Lizard")]
        Dathomir_HandlersGiantBrownLizard = 259,
        [NPCGroup("Old Giant Brown Lizard")]
        Dathomir_OldGiantBrownLizard = 261,
        [NPCGroup("Handler's Wraid")]
        Dathomir_HandlersWraid = 264,
        [NPCGroup("Old Wraid")]
        Dathomir_OldWraid = 266,

        // Retain the historical groups above for persisted enum keys and migration.
        [NPCGroup("Trial Drill Droids")]
        Hutlar_TrialDrillDroids = 267,
        [NPCGroup("Trial Duelist Droids")]
        Hutlar_TrialDuelistDroids = 268,
        [NPCGroup("Trial Gatekeeper Droid")]
        Hutlar_TrialGatekeeperDroid = 269,
        [NPCGroup("Trial Examiner Droids")]
        Hutlar_TrialExaminerDroids = 270,
        [NPCGroup("Tempest Training Engine")]
        Hutlar_TempestTrainingEngine = 271,
        [NPCGroup("Bastion Training Engine")]
        Hutlar_BastionTrainingEngine = 276,
        [NPCGroup("Vortex Training Engine")]
        Hutlar_VortexTrainingEngine = 281,
        [NPCGroup("Expedition Raiders")]
        MonCala_ExpeditionRaiders = 282,
        [NPCGroup("Expedition Marauders")]
        MonCala_ExpeditionMarauders = 283,
        [NPCGroup("Expedition Captain Rhaan Tolek")]
        MonCala_ExpeditionCaptainRhaanTolek = 284,
        [NPCGroup("Expedition Reavers")]
        MonCala_ExpeditionReavers = 285,
        [NPCGroup("Expedition Leader Kaelen Dorr")]
        MonCala_ExpeditionLeaderKaelenDorr = 286,
        [NPCGroup("Expedition Captain Zarrik Thane")]
        MonCala_ExpeditionCaptainZarrikThane = 289,
        [NPCGroup("Expedition Leader Mordek Sull")]
        MonCala_ExpeditionLeaderMordekSull = 291,
        [NPCGroup("Expedition Captain Tavik Orrun")]
        MonCala_ExpeditionCaptainTavikOrrun = 294,
        [NPCGroup("Expedition Leader Lorrek Syn")]
        MonCala_ExpeditionLeaderLorrekSyn = 296,
        [NPCGroup("Foundry Droid")]
        Hutlar_FoundryDroid = 297,
        [NPCGroup("Station Horror")]
        AbandonedStation_Horror = 298,
        [NPCGroup("Krakenborne Ship")]
        MonCala_Krakenborne = 311,

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
