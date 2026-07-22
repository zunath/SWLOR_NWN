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
        [NPCGroup("Invincible Adepts")]
        Viscara_Invincible_Adept = 72,
        [NPCGroup("Invincible Specialists")]
        Viscara_Invincible_Specialist = 73,
        [NPCGroup("Invincible Warden")]
        Viscara_Invincible_Warden = 74,
        [NPCGroup("Invincible Inner Circle")]
        Viscara_Invincible_InnerCircle = 75,
        [NPCGroup("Invincible Master")]
        Viscara_Invincible_Master = 76,
        [NPCGroup("Vital Rupture Adepts")]
        Viscara_VitalRupture_Adept = 77,
        [NPCGroup("Vital Rupture Specialists")]
        Viscara_VitalRupture_Specialist = 78,
        [NPCGroup("Vital Rupture Warden")]
        Viscara_VitalRupture_Warden = 79,
        [NPCGroup("Vital Rupture Inner Circle")]
        Viscara_VitalRupture_InnerCircle = 80,
        [NPCGroup("Vital Rupture Master")]
        Viscara_VitalRupture_Master = 81,
        [NPCGroup("Systemic Shutdown Adepts")]
        Viscara_SystemicShutdown_Adept = 82,
        [NPCGroup("Systemic Shutdown Specialists")]
        Viscara_SystemicShutdown_Specialist = 83,
        [NPCGroup("Systemic Shutdown Warden")]
        Viscara_SystemicShutdown_Warden = 84,
        [NPCGroup("Systemic Shutdown Inner Circle")]
        Viscara_SystemicShutdown_InnerCircle = 85,
        [NPCGroup("Systemic Shutdown Master")]
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
        [NPCGroup("Absolute Defense Adepts")]
        Korriban_AbsoluteDefense_Adept = 102,
        [NPCGroup("Absolute Defense Specialists")]
        Korriban_AbsoluteDefense_Specialist = 103,
        [NPCGroup("Absolute Defense Warden")]
        Korriban_AbsoluteDefense_Warden = 104,
        [NPCGroup("Absolute Defense Inner Circle")]
        Korriban_AbsoluteDefense_InnerCircle = 105,
        [NPCGroup("Absolute Defense Master")]
        Korriban_AbsoluteDefense_Master = 106,
        [NPCGroup("Soul Ascension Adepts")]
        Korriban_SoulAscension_Adept = 107,
        [NPCGroup("Soul Ascension Specialists")]
        Korriban_SoulAscension_Specialist = 108,
        [NPCGroup("Soul Ascension Warden")]
        Korriban_SoulAscension_Warden = 109,
        [NPCGroup("Soul Ascension Inner Circle")]
        Korriban_SoulAscension_InnerCircle = 110,
        [NPCGroup("Soul Ascension Master")]
        Korriban_SoulAscension_Master = 111,
        [NPCGroup("Forcebane Adepts")]
        Korriban_Forcebane_Adept = 112,
        [NPCGroup("Forcebane Specialists")]
        Korriban_Forcebane_Specialist = 113,
        [NPCGroup("Forcebane Warden")]
        Korriban_Forcebane_Warden = 114,
        [NPCGroup("Forcebane Inner Circle")]
        Korriban_Forcebane_InnerCircle = 115,
        [NPCGroup("Forcebane Master")]
        Korriban_Forcebane_Master = 116,
        [NPCGroup("Crippling Defense Adepts")]
        SmugglersMoon_CripplingDefense_Adept = 117,
        [NPCGroup("Crippling Defense Specialists")]
        SmugglersMoon_CripplingDefense_Specialist = 118,
        [NPCGroup("Crippling Defense Warden")]
        SmugglersMoon_CripplingDefense_Warden = 119,
        [NPCGroup("Crippling Defense Inner Circle")]
        SmugglersMoon_CripplingDefense_InnerCircle = 120,
        [NPCGroup("Crippling Defense Master")]
        SmugglersMoon_CripplingDefense_Master = 121,
        [NPCGroup("Tempest Bloom Adepts")]
        SmugglersMoon_TempestBloom_Adept = 122,
        [NPCGroup("Tempest Bloom Specialists")]
        SmugglersMoon_TempestBloom_Specialist = 123,
        [NPCGroup("Tempest Bloom Warden")]
        SmugglersMoon_TempestBloom_Warden = 124,
        [NPCGroup("Tempest Bloom Inner Circle")]
        SmugglersMoon_TempestBloom_InnerCircle = 125,
        [NPCGroup("Tempest Bloom Master")]
        SmugglersMoon_TempestBloom_Master = 126,
        [NPCGroup("Red Bloom Adepts")]
        SmugglersMoon_RedBloom_Adept = 127,
        [NPCGroup("Red Bloom Specialists")]
        SmugglersMoon_RedBloom_Specialist = 128,
        [NPCGroup("Red Bloom Warden")]
        SmugglersMoon_RedBloom_Warden = 129,
        [NPCGroup("Red Bloom Inner Circle")]
        SmugglersMoon_RedBloom_InnerCircle = 130,
        [NPCGroup("Red Bloom Master")]
        SmugglersMoon_RedBloom_Master = 131,
        [NPCGroup("Adamantine Guard Adepts")]
        CZ220_AdamantineGuard_Adept = 132,
        [NPCGroup("Adamantine Guard Specialists")]
        CZ220_AdamantineGuard_Specialist = 133,
        [NPCGroup("Adamantine Guard Warden")]
        CZ220_AdamantineGuard_Warden = 134,
        [NPCGroup("Adamantine Guard Inner Circle")]
        CZ220_AdamantineGuard_InnerCircle = 135,
        [NPCGroup("Adamantine Guard Master")]
        CZ220_AdamantineGuard_Master = 136,
        [NPCGroup("Scrapheap Lockdown Adepts")]
        CZ220_ScrapheapLockdown_Adept = 137,
        [NPCGroup("Scrapheap Lockdown Specialists")]
        CZ220_ScrapheapLockdown_Specialist = 138,
        [NPCGroup("Scrapheap Lockdown Warden")]
        CZ220_ScrapheapLockdown_Warden = 139,
        [NPCGroup("Scrapheap Lockdown Inner Circle")]
        CZ220_ScrapheapLockdown_InnerCircle = 140,
        [NPCGroup("Scrapheap Lockdown Master")]
        CZ220_ScrapheapLockdown_Master = 141,
        [NPCGroup("Worldbreaker Adepts")]
        CZ220_Worldbreaker_Adept = 142,
        [NPCGroup("Worldbreaker Specialists")]
        CZ220_Worldbreaker_Specialist = 143,
        [NPCGroup("Worldbreaker Warden")]
        CZ220_Worldbreaker_Warden = 144,
        [NPCGroup("Worldbreaker Inner Circle")]
        CZ220_Worldbreaker_InnerCircle = 145,
        [NPCGroup("Worldbreaker Master")]
        CZ220_Worldbreaker_Master = 146,
        [NPCGroup("Unmoving Center Adepts")]
        Tatooine_UnmovingCenter_Adept = 147,
        [NPCGroup("Unmoving Center Specialists")]
        Tatooine_UnmovingCenter_Specialist = 148,
        [NPCGroup("Unmoving Center Warden")]
        Tatooine_UnmovingCenter_Warden = 149,
        [NPCGroup("Unmoving Center Inner Circle")]
        Tatooine_UnmovingCenter_InnerCircle = 150,
        [NPCGroup("Unmoving Center Master")]
        Tatooine_UnmovingCenter_Master = 151,
        [NPCGroup("Last Word Adepts")]
        Tatooine_LastWord_Adept = 152,
        [NPCGroup("Last Word Specialists")]
        Tatooine_LastWord_Specialist = 153,
        [NPCGroup("Last Word Warden")]
        Tatooine_LastWord_Warden = 154,
        [NPCGroup("Last Word Inner Circle")]
        Tatooine_LastWord_InnerCircle = 155,
        [NPCGroup("Last Word Master")]
        Tatooine_LastWord_Master = 156,
        [NPCGroup("Dead Man's Hand Adepts")]
        Tatooine_DeadMansHand_Adept = 157,
        [NPCGroup("Dead Man's Hand Specialists")]
        Tatooine_DeadMansHand_Specialist = 158,
        [NPCGroup("Dead Man's Hand Warden")]
        Tatooine_DeadMansHand_Warden = 159,
        [NPCGroup("Dead Man's Hand Inner Circle")]
        Tatooine_DeadMansHand_InnerCircle = 160,
        [NPCGroup("Dead Man's Hand Master")]
        Tatooine_DeadMansHand_Master = 161,
        [NPCGroup("Kill Box Adepts")]
        SmugglersMoon_KillBox_Adept = 162,
        [NPCGroup("Kill Box Specialists")]
        SmugglersMoon_KillBox_Specialist = 163,
        [NPCGroup("Kill Box Warden")]
        SmugglersMoon_KillBox_Warden = 164,
        [NPCGroup("Kill Box Inner Circle")]
        SmugglersMoon_KillBox_InnerCircle = 165,
        [NPCGroup("Kill Box Master")]
        SmugglersMoon_KillBox_Master = 166,
        [NPCGroup("One Shot Adepts")]
        SmugglersMoon_OneShot_Adept = 167,
        [NPCGroup("One Shot Specialists")]
        SmugglersMoon_OneShot_Specialist = 168,
        [NPCGroup("One Shot Warden")]
        SmugglersMoon_OneShot_Warden = 169,
        [NPCGroup("One Shot Inner Circle")]
        SmugglersMoon_OneShot_InnerCircle = 170,
        [NPCGroup("One Shot Master")]
        SmugglersMoon_OneShot_Master = 171,
        [NPCGroup("Rain of Steel Adepts")]
        SmugglersMoon_RainOfSteel_Adept = 172,
        [NPCGroup("Rain of Steel Specialists")]
        SmugglersMoon_RainOfSteel_Specialist = 173,
        [NPCGroup("Rain of Steel Warden")]
        SmugglersMoon_RainOfSteel_Warden = 174,
        [NPCGroup("Rain of Steel Inner Circle")]
        SmugglersMoon_RainOfSteel_InnerCircle = 175,
        [NPCGroup("Rain of Steel Master")]
        SmugglersMoon_RainOfSteel_Master = 176,
        [NPCGroup("Perfect Flurry Adepts")]
        Hutlar_PerfectFlurry_Adept = 177,
        [NPCGroup("Perfect Flurry Specialists")]
        Hutlar_PerfectFlurry_Specialist = 178,
        [NPCGroup("Perfect Flurry Warden")]
        Hutlar_PerfectFlurry_Warden = 179,
        [NPCGroup("Perfect Flurry Inner Circle")]
        Hutlar_PerfectFlurry_InnerCircle = 180,
        [NPCGroup("Perfect Flurry Master")]
        Hutlar_PerfectFlurry_Master = 181,
        [NPCGroup("Thermal Detonator Adepts")]
        Hutlar_ThermalDetonator_Adept = 182,
        [NPCGroup("Thermal Detonator Specialists")]
        Hutlar_ThermalDetonator_Specialist = 183,
        [NPCGroup("Thermal Detonator Warden")]
        Hutlar_ThermalDetonator_Warden = 184,
        [NPCGroup("Thermal Detonator Inner Circle")]
        Hutlar_ThermalDetonator_InnerCircle = 185,
        [NPCGroup("Thermal Detonator Master")]
        Hutlar_ThermalDetonator_Master = 186,
        [NPCGroup("Overload Barrage Adepts")]
        Hutlar_OverloadBarrage_Adept = 187,
        [NPCGroup("Overload Barrage Specialists")]
        Hutlar_OverloadBarrage_Specialist = 188,
        [NPCGroup("Overload Barrage Warden")]
        Hutlar_OverloadBarrage_Warden = 189,
        [NPCGroup("Overload Barrage Inner Circle")]
        Hutlar_OverloadBarrage_InnerCircle = 190,
        [NPCGroup("Overload Barrage Master")]
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
        [NPCGroup("Killzone Beacon Adepts")]
        Viscara_KillzoneBeacon_Adept = 207,
        [NPCGroup("Killzone Beacon Specialists")]
        Viscara_KillzoneBeacon_Specialist = 208,
        [NPCGroup("Killzone Beacon Warden")]
        Viscara_KillzoneBeacon_Warden = 209,
        [NPCGroup("Killzone Beacon Inner Circle")]
        Viscara_KillzoneBeacon_InnerCircle = 210,
        [NPCGroup("Killzone Beacon Master")]
        Viscara_KillzoneBeacon_Master = 211,
        [NPCGroup("Emergency Bunker Adepts")]
        Viscara_EmergencyBunker_Adept = 212,
        [NPCGroup("Emergency Bunker Specialists")]
        Viscara_EmergencyBunker_Specialist = 213,
        [NPCGroup("Emergency Bunker Warden")]
        Viscara_EmergencyBunker_Warden = 214,
        [NPCGroup("Emergency Bunker Inner Circle")]
        Viscara_EmergencyBunker_InnerCircle = 215,
        [NPCGroup("Emergency Bunker Master")]
        Viscara_EmergencyBunker_Master = 216,
        [NPCGroup("Decisive Command Adepts")]
        Viscara_DecisiveCommand_Adept = 217,
        [NPCGroup("Decisive Command Specialists")]
        Viscara_DecisiveCommand_Specialist = 218,
        [NPCGroup("Decisive Command Warden")]
        Viscara_DecisiveCommand_Warden = 219,
        [NPCGroup("Decisive Command Inner Circle")]
        Viscara_DecisiveCommand_InnerCircle = 220,
        [NPCGroup("Decisive Command Master")]
        Viscara_DecisiveCommand_Master = 221,
        [NPCGroup("Hold the Line Adepts")]
        Dantooine_HoldTheLine_Adept = 222,
        [NPCGroup("Hold the Line Specialists")]
        Dantooine_HoldTheLine_Specialist = 223,
        [NPCGroup("Hold the Line Warden")]
        Dantooine_HoldTheLine_Warden = 224,
        [NPCGroup("Hold the Line Inner Circle")]
        Dantooine_HoldTheLine_InnerCircle = 225,
        [NPCGroup("Hold the Line Master")]
        Dantooine_HoldTheLine_Master = 226,
        [NPCGroup("Emergency Cocktail Adepts")]
        Dantooine_EmergencyCocktail_Adept = 227,
        [NPCGroup("Emergency Cocktail Specialists")]
        Dantooine_EmergencyCocktail_Specialist = 228,
        [NPCGroup("Emergency Cocktail Warden")]
        Dantooine_EmergencyCocktail_Warden = 229,
        [NPCGroup("Emergency Cocktail Inner Circle")]
        Dantooine_EmergencyCocktail_InnerCircle = 230,
        [NPCGroup("Emergency Cocktail Master")]
        Dantooine_EmergencyCocktail_Master = 231,
        [NPCGroup("Infinite Conduit Adepts")]
        Dantooine_InfiniteConduit_Adept = 232,
        [NPCGroup("Infinite Conduit Specialists")]
        Dantooine_InfiniteConduit_Specialist = 233,
        [NPCGroup("Infinite Conduit Warden")]
        Dantooine_InfiniteConduit_Warden = 234,
        [NPCGroup("Infinite Conduit Inner Circle")]
        Dantooine_InfiniteConduit_InnerCircle = 235,
        [NPCGroup("Infinite Conduit Master")]
        Dantooine_InfiniteConduit_Master = 236,
        [NPCGroup("Apex Bite Adepts")]
        Dathomir_ApexBite_Adept = 237,
        [NPCGroup("Apex Bite Specialists")]
        Dathomir_ApexBite_Specialist = 238,
        [NPCGroup("Apex Bite Warden")]
        Dathomir_ApexBite_Warden = 239,
        [NPCGroup("Apex Bite Inner Circle")]
        Dathomir_ApexBite_InnerCircle = 240,
        [NPCGroup("Apex Bite Master")]
        Dathomir_ApexBite_Master = 241,
        [NPCGroup("Unbreakable Beast Adepts")]
        Dathomir_UnbreakableBeast_Adept = 242,
        [NPCGroup("Unbreakable Beast Specialists")]
        Dathomir_UnbreakableBeast_Specialist = 243,
        [NPCGroup("Unbreakable Beast Warden")]
        Dathomir_UnbreakableBeast_Warden = 244,
        [NPCGroup("Unbreakable Beast Inner Circle")]
        Dathomir_UnbreakableBeast_InnerCircle = 245,
        [NPCGroup("Unbreakable Beast Master")]
        Dathomir_UnbreakableBeast_Master = 246,
        [NPCGroup("Alpha Rhythm Adepts")]
        Dathomir_AlphaRhythm_Adept = 247,
        [NPCGroup("Alpha Rhythm Specialists")]
        Dathomir_AlphaRhythm_Specialist = 248,
        [NPCGroup("Alpha Rhythm Warden")]
        Dathomir_AlphaRhythm_Warden = 249,
        [NPCGroup("Alpha Rhythm Inner Circle")]
        Dathomir_AlphaRhythm_InnerCircle = 250,
        [NPCGroup("Alpha Rhythm Master")]
        Dathomir_AlphaRhythm_Master = 251,
        [NPCGroup("Primal Overrun Adepts")]
        Dathomir_PrimalOverrun_Adept = 252,
        [NPCGroup("Primal Overrun Specialists")]
        Dathomir_PrimalOverrun_Specialist = 253,
        [NPCGroup("Primal Overrun Warden")]
        Dathomir_PrimalOverrun_Warden = 254,
        [NPCGroup("Primal Overrun Inner Circle")]
        Dathomir_PrimalOverrun_InnerCircle = 255,
        [NPCGroup("Primal Overrun Master")]
        Dathomir_PrimalOverrun_Master = 256,
        [NPCGroup("Untouchable Instinct Adepts")]
        Dathomir_UntouchableInstinct_Adept = 257,
        [NPCGroup("Untouchable Instinct Specialists")]
        Dathomir_UntouchableInstinct_Specialist = 258,
        [NPCGroup("Untouchable Instinct Warden")]
        Dathomir_UntouchableInstinct_Warden = 259,
        [NPCGroup("Untouchable Instinct Inner Circle")]
        Dathomir_UntouchableInstinct_InnerCircle = 260,
        [NPCGroup("Untouchable Instinct Master")]
        Dathomir_UntouchableInstinct_Master = 261,
        [NPCGroup("Force-Bonded Beast Adepts")]
        Dathomir_ForceBondedBeast_Adept = 262,
        [NPCGroup("Force-Bonded Beast Specialists")]
        Dathomir_ForceBondedBeast_Specialist = 263,
        [NPCGroup("Force-Bonded Beast Warden")]
        Dathomir_ForceBondedBeast_Warden = 264,
        [NPCGroup("Force-Bonded Beast Inner Circle")]
        Dathomir_ForceBondedBeast_InnerCircle = 265,
        [NPCGroup("Force-Bonded Beast Master")]
        Dathomir_ForceBondedBeast_Master = 266,
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
