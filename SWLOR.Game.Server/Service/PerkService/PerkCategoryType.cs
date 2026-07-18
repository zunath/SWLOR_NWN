namespace SWLOR.Game.Server.Service.PerkService
{
    public enum PerkCategoryType
    {
        [PerkCategory("Invalid", false)]
        Invalid = 0,

        [PerkCategory("Force - Alter", true)]
        ForceAlter = 1,

        [PerkCategory("General", true)]
        General = 2,

        [PerkCategory("Piloting", true)]
        Piloting = 5,

        [PerkCategory("First Aid", false)]
        FirstAid = 6,

        [PerkCategory("Smithery", true)]
        Smithery = 7,

        [PerkCategory("Fabrication", true)]
        Fabrication = 8,

        [PerkCategory("Gathering", true)]
        Gathering = 9,

        [PerkCategory("Leadership", true)]
        Leadership = 10,

        [PerkCategory("Force - Control", true)]
        ForceControl = 11,

        [PerkCategory("Force - Sense", true)]
        ForceSense = 12,

        [PerkCategory("Agriculture", true)]
        Agriculture = 13,

        [PerkCategory("Engineering", true)]
        Engineering = 14,

        [PerkCategory("Devices", false)]
        Devices = 15,

        [PerkCategory("Beast Mastery - Training", true)]
        BeastMasteryTraining = 16,

        [PerkCategory("Beast Mastery - Incubation", true)]
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

        [PerkCategory("Vibroblade - Bulwark", true)]
        VibrobladeDefense = 25,

        [PerkCategory("Vibroblade - Frenzy", true)]
        VibrobladeOffense = 26,

        [PerkCategory("Vibroknife - Shadow", true)]
        VibroknifeShadow = 27,

        [PerkCategory("Vibroknife - Saboteur", true)]
        VibroknifeSaboteur = 28,

        [PerkCategory("Lightsaber - Severance", true)]
        LightsaberDefense = 29,

        [PerkCategory("Lightsaber - Ward", true)]
        LightsaberOffense = 30,

        [PerkCategory("Heavy Vibroblade - Immortal", true)]
        HeavyVibrobladeDefense = 31,

        [PerkCategory("Heavy Vibroblade - Berserker", true)]
        HeavyVibrobladeOffense = 32,

        [PerkCategory("Spear - Vigor", true)]
        SpearDamage = 33,

        [PerkCategory("Spear - Disabler", true)]
        SpearDisabler = 34,

        [PerkCategory("Twin Blade - Cyclone", true)]
        TwinBladeCyclone = 35,

        [PerkCategory("Twin Blade - Lacerator", true)]
        TwinBladeDuelist = 36,

        [PerkCategory("Saberstaff - Conduit", true)]
        SaberstaffConduit = 37,

        [PerkCategory("Saberstaff - Tempest", true)]
        SaberstaffTempest = 38,

        [PerkCategory("Katar - Iron Guard", true)]
        KatarIronGuard = 39,

        [PerkCategory("Katar - Scrapper", true)]
        KatarVenomCurrent = 40,

        [PerkCategory("Staff - Crusher", true)]
        StaffCrusher = 41,

        [PerkCategory("Staff - Sentinel", true)]
        StaffSentinel = 42,

        [PerkCategory("Pistol - Gambler", true)]
        PistolGunslinger = 43,

        [PerkCategory("Pistol - Skirmisher", true)]
        PistolSkirmisher = 44,

        [PerkCategory("Rifle - Marksman", true)]
        RifleMarksman = 45,

        [PerkCategory("Rifle - Suppression", true)]
        RiflePacification = 46,

        [PerkCategory("Throwing - Ordnance", true)]
        ThrowingBombardier = 47,

        [PerkCategory("Throwing - Flurry", true)]
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

        [PerkCategory("First Aid - Trauma Medic", true)]
        FirstAidTraumaMedic = 55,

        [PerkCategory("First Aid - Combat Pharmacology", true)]
        FirstAidCombatPharmacology = 56,

        [PerkCategory("Mimicry", true)]
        Mimicry = 57,

        [PerkCategory("Espionage - Infiltrator", true)]
        EspionageInfiltrator = 58,

        [PerkCategory("Espionage - Saboteur", true)]
        EspionageSaboteur = 59,

        [PerkCategory("Espionage - Tradecraft", true)]
        EspionageTradecraft = 60,
    }

    public class PerkCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public PerkCategoryAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
