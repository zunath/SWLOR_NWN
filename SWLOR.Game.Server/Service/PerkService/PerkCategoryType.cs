using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.PerkService
{
    public enum PerkCategoryType
    {
        [PerkCategory("Invalid", false)]
        Invalid = 0,

        [PerkCategory("Force - Alter", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        ForceAlter = 1,

        [PerkCategory("General", true)]
        General = 2,

        [PerkCategory("Piloting", false)]
        Piloting = 5,

        [PerkCategory("First Aid", false, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        FirstAid = 6,

        [PerkCategory("Smithery", true)]
        Smithery = 7,

        [PerkCategory("Fabrication", true)]
        Fabrication = 8,

        [PerkCategory("Gathering", true)]
        Gathering = 9,

        [PerkCategory("Leadership", true)]
        Leadership = 10,

        [PerkCategory("Force - Control", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        ForceControl = 11,

        [PerkCategory("Force - Sense", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        ForceSense = 12,

        [PerkCategory("Agriculture", true)]
        Agriculture = 13,

        [PerkCategory("Engineering", true)]
        Engineering = 14,

        [PerkCategory("Devices", false)]
        Devices = 15,

        [PerkCategory("Beast Mastery - Training", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        BeastMasteryTraining = 16,

        [PerkCategory("Beast Mastery - Incubation", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        BeastMasteryIncubation = 17,

        [PerkCategory("Beast - Damage", true)]
        BeastDamage = 19,

        [PerkCategory("Beast - Tank", true)]
        BeastTank = 20,

        [PerkCategory("Beast - Balanced", true)]
        BeastBalanced = 21,

        [PerkCategory("Beast - Bruiser", true)]
        BeastBruiser = 22,

        [PerkCategory("Beast - Evasion", true)]
        BeastEvasion = 23,

        [PerkCategory("Beast - Force", true)]
        BeastForce = 24,

        [PerkCategory("Vibroblade - Bulwark", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        VibrobladeDefense = 25,

        [PerkCategory("Vibroblade - Frenzy", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        VibrobladeOffense = 26,

        [PerkCategory("Vibroknife - Shadow", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        VibroknifeShadow = 27,

        [PerkCategory("Vibroknife - Saboteur", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        VibroknifeSaboteur = 28,

        [PerkCategory("Lightsaber - Severance", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        LightsaberDefense = 29,

        [PerkCategory("Lightsaber - Ward", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        LightsaberOffense = 30,

        [PerkCategory("Heavy Vibroblade - Immortal", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        HeavyVibrobladeDefense = 31,

        [PerkCategory("Heavy Vibroblade - Berserker", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        HeavyVibrobladeOffense = 32,

        [PerkCategory("Spear - Vigor", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        SpearDamage = 33,

        [PerkCategory("Spear - Disabler", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        SpearDisabler = 34,

        [PerkCategory("Twin Blade - Cyclone", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        TwinBladeCyclone = 35,

        [PerkCategory("Twin Blade - Lacerator", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        TwinBladeDuelist = 36,

        [PerkCategory("Saberstaff - Conduit", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        SaberstaffConduit = 37,

        [PerkCategory("Saberstaff - Tempest", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        SaberstaffTempest = 38,

        [PerkCategory("Katar - Iron Guard", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        KatarIronGuard = 39,

        [PerkCategory("Katar - Scrapper", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        KatarVenomCurrent = 40,

        [PerkCategory("Staff - Crusher", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        StaffCrusher = 41,

        [PerkCategory("Staff - Sentinel", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        StaffSentinel = 42,

        [PerkCategory("Pistol - Gambler", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        PistolGunslinger = 43,

        [PerkCategory("Pistol - Skirmisher", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        PistolSkirmisher = 44,

        [PerkCategory("Rifle - Marksman", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        RifleMarksman = 45,

        [PerkCategory("Rifle - Suppression", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        RiflePacification = 46,

        [PerkCategory("Throwing - Ordnance", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        ThrowingBombardier = 47,

        [PerkCategory("Throwing - Flurry", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        ThrowingDeadeye = 48,

        [PerkCategory("Leadership - Vanguard Command", true)]
        LeadershipVanguardCommand = 49,

        [PerkCategory("Leadership - Field Steward", true)]
        LeadershipFieldSteward = 50,

        [PerkCategory("Devices - Grenadier", true)]
        DevicesGrenadier = 51,

        [PerkCategory("Devices - Field Engineer", true)]
        DevicesFieldEngineer = 52,

        [PerkCategory("Devices - Field Support", true)]
        DevicesFieldSupport = 53,

        [PerkCategory("Devices - Assault Gadgets", true)]
        DevicesAssaultGadgets = 54,

        [PerkCategory("First Aid - Trauma Medic", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        FirstAidTraumaMedic = 55,

        [PerkCategory("First Aid - Combat Pharmacology", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        FirstAidCombatPharmacology = 56,

        [PerkCategory("Mimicry", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Mimicry = 57,

        [PerkCategory("Espionage - Infiltrator", true)]
        EspionageInfiltrator = 58,

        [PerkCategory("Espionage - Saboteur", true)]
        EspionageSaboteur = 59,

        [PerkCategory("Espionage - Tradecraft", true)]
        EspionageTradecraft = 60,

        [PerkCategory("Unarmed - Martial Artist", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        UnarmedMartialArtist = 61,

        [PerkCategory("Unarmed - Brawler", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        UnarmedBrawler = 62,

        [PerkCategory("Twin Pistol - Barrage", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        TwinPistolBarrage = 63,

        [PerkCategory("Twin Pistol - Crossfire", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        TwinPistolCrossfire = 64,

        [PerkCategory("Piloting - Ace", true)]
        PilotingAce = 65,

        [PerkCategory("Piloting - Ordnance", true)]
        PilotingOrdnance = 66,

        [PerkCategory("Piloting - Bulwark", true)]
        PilotingBulwark = 67,

        [PerkCategory("Piloting - Command", true)]
        PilotingCommand = 68,

        [PerkCategory("Droid Protocols", true)]
        DroidProtocols = 69,

        [PerkCategory("Performance - Dancing", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        PerformanceDancing = 75,

        [PerkCategory("Performance - Instruments", true, BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        PerformanceInstruments = 76,
        [PerkCategory("Utility Systems - Repair", true, BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]
        UtilitySystemsRepair = 77,
        [PerkCategory("Utility Systems - Augments", true, BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]
        UtilitySystemsAugments = 78,
        [PerkCategory("Armaments - Shock Prod", true, BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]
        ArmamentsShockProd = 79,
        [PerkCategory("Armaments - Blaster Mount", true, BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]
        ArmamentsBlasterMount = 80,
    }

    public class PerkCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Limits the perk line to one body frame. Astromech lines are hidden from humanoid characters and
        /// play the chassis's own clips instead of the authored humanoid animations.
        /// </summary>
        public BodyFrameRestriction BodyFrameRestriction { get; set; }

        public PerkCategoryAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
