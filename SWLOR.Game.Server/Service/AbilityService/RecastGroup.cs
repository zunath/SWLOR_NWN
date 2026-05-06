using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    // Note: Short names are what's displayed on the recast Gui element. They are limited to 14 characters.
    public enum RecastGroup
    {
        [RecastGroup("Invalid", "Invalid", false)]
        Invalid = 0,
        [RecastGroup("Burst Of Speed", "Burst Of Speed", true)]
        BurstOfSpeed = 1,
        [RecastGroup("Force Heal", "Force Heal", true)]
        ForceHeal = 2,
        [RecastGroup("Force Push", "Force Push", true)]
        ForcePush = 3,
        [RecastGroup("Throw Lightsaber", "Throw Saber", true)]
        ThrowLightsaber = 4,
        [RecastGroup("Force Stun", "Force Stun", true)]
        ForceStun = 5,
        [RecastGroup("Battle Insight", "Battle Insight", true)]
        BattleInsight = 6,
        [RecastGroup("Comprehend Speech", "Comp. Speech", true)]
        ComprehendSpeech = 7,
        [RecastGroup("Mind Trick", "Mind Trick", true)]
        MindTrick = 8,
        [RecastGroup("Force Burst", "Force Burst", true)]
        ForceBurst = 9,
        [RecastGroup("Force Body", "Force Body", true)]
        ForceBody = 10,
        [RecastGroup("Force Drain", "Force Drain", true)]
        ForceDrain = 11,
        [RecastGroup("Force Lightning", "F. Lightning", true)]
        ForceLightning = 12,
        [RecastGroup("Force Mind", "Force Mind", true)]
        ForceMind = 13,
        [RecastGroup("Force Leap", "Force Leap", true)]
        ForceLeap = 14,
        [RecastGroup("Frag Grenade", "Frag Grenade", true)]
        FragGrenade = 15,
        [RecastGroup("Rest", "Rest", true)]
        Rest = 16,
        [RecastGroup("Roar", "Roar", true)]
        Roar = 17,
        [RecastGroup("Bite", "Bite", true)]
        Bite = 18,
        [RecastGroup("Iron Shell", "Iron Shell", true)]
        IronShell = 19,
        [RecastGroup("Earthquake", "Earthquake", true)]
        Earthquake = 20,
        [RecastGroup("Fire Breath", "F. Breath", true)]
        FireBreath = 21,
        [RecastGroup("Spikes", "Spikes", true)]
        Spikes = 22,
        [RecastGroup("Venom", "Venom", true)]
        Venom = 23,
        [RecastGroup("Talon", "Talon", true)]
        Talon = 24,
        [RecastGroup("Med Kit", "Med Kit", true)]
        MedKit = 25,
        [RecastGroup("Kolto Recovery", "K. Recovery", true)]
        KoltoRecovery = 26,
        [RecastGroup("Resuscitation", "Resuscitation", true)]
        Resuscitation = 27,
        [RecastGroup("Treatment Kit", "Treatment Kit", true)]
        TreatmentKit = 28,
        [RecastGroup("Stasis Field", "Stasis Field", true)]
        StasisField = 29,
        [RecastGroup("Combat Enhancement", "Combat En.", true)]
        CombatEnhancement = 30,
        [RecastGroup("Shielding", "Shielding", true)]
        Shielding = 31,
        [RecastGroup("Bombs", "Bombs", true)]
        Bombs = 32,
        [RecastGroup("Stealth Generator", "Stealth Gen.", true)]
        StealthGenerator = 33,
        [RecastGroup("Flamethrower", "Flamethrower", true)]
        Flamethrower = 34,
        [RecastGroup("Wrist Rocket", "W. Rocket", true)]
        WristRocket = 35,
        [RecastGroup("Deflector Shield", "D. Shield", true)]
        DeflectorShield = 36,
        [RecastGroup("Provoke", "Provoke", true)]
        Provoke = 37,
        [RecastGroup("Provoke II", "Provoke II", true)]
        Provoke2 = 38,
        [RecastGroup("Premonition", "Premonition", true)]
        Premonition = 39,
        [RecastGroup("Disturbance", "Disturbance", true)]
        Disturbance = 40,
        [RecastGroup("Benevolence", "Benevolence", true)]
        Benevolence = 41,
        [RecastGroup("Force Valor", "F. Valor", true)]
        ForceValor = 42,
        [RecastGroup("Force Spark", "F. Spark", true)]
        ForceSpark = 43,
        [RecastGroup("Creeping Terror", "C. Terror", true)]
        CreepingTerror = 44,
        [RecastGroup("Force Rage", "F. Rage", true)]
        ForceRage = 45,
        [RecastGroup("Furor", "Furor", true)]
        Furor = 46,
        [RecastGroup("Throw Rock", "Throw Rock", true)]
        ThrowRock = 47,
        [RecastGroup("Force Inspiration", "F. Inspiration", true)]
        ForceInspiration = 48,
        [RecastGroup("Rousing Shout", "R. Shout", true)]
        RousingShout = 49,
        [RecastGroup("Dedication", "Dedication", true)]
        Dedication = 50,
        [RecastGroup("Soldier's Speed", "Sol. Speed", true)]
        SoldiersSpeed = 51,
        [RecastGroup("Soldier's Strike", "Sol. Strike", true)]
        SoldiersStrike = 52,
        [RecastGroup("Charge", "Charge", true)]
        Charge = 53,
        [RecastGroup("Soldier's Precisionn", "Sol. Precision", true)]
        SoldiersPrecision = 54,
        [RecastGroup("Shocking Shout", "Shock. Shout", true)]
        ShockingShout = 55,
        [RecastGroup("Rejuvenation", "Rejuvenation", true)]
        Rejuvenation = 56,
        [RecastGroup("Frenzied Shout", "Frenz. Shout", true)]
        FrenziedShout = 57,
        [RecastGroup("Screech", "Screech", true)]
        Screech = 58,
        [RecastGroup("Flame Blast", "F. Blast", true)]
        FlameBlast = 59,
        [RecastGroup("Greater Earthquake", "G. Quake", true)]
        GreaterEarthquake = 60,
        [RecastGroup("Infusion", "Infusion", true)]
        Infusion = 61,
        [RecastGroup("Droid Controller", "Droid Contr.", true)]
        DroidController = 62,
        [RecastGroup("Stat Rebuild", "Stat Rebuild", false)]
        StatRebuild = 63,
        [RecastGroup("Concussion Grenade", "Conc. Grenade", true)]
        ConcussionGrenade = 64,
        [RecastGroup("Flashbang Grenade", "Flash. Grenade", true)]
        FlashbangGrenade = 65,
        [RecastGroup("Ion Grenade", "Ion Grenade", true)]
        IonGrenade = 66,
        [RecastGroup("Kolto Grenade", "Kolto Grenade", true)]
        KoltoGrenade = 67,
        [RecastGroup("Adhesive Grenade", "Adhe. Grenade", true)]
        AdhesiveGrenade = 68,
        [RecastGroup("Smoke Bomb", "Smoke Bomb", true)]
        SmokeBomb = 69,
        [RecastGroup("Kolto Bomb", "Kolto Bomb", true)]
        KoltoBomb = 70,
        [RecastGroup("Incendiary Bomb", "Incen. Bomb", true)]
        IncendiaryBomb = 71,
        [RecastGroup("Gas Bomb", "Gas Bomb", true)]
        GasBomb = 72,
        [RecastGroup("Tame", "Tame", true)]
        Tame = 73,
        [RecastGroup("Reward", "Reward", true)]
        Reward = 74,
        [RecastGroup("Snarl/Growl", "Snarl/Growl", true)]
        SnarlGrowl = 75,
        [RecastGroup("Soothe Pet", "Soothe Pet", true)]
        SoothePet = 76,
        [RecastGroup("Revive Beast", "Rev. Beast", true)]
        ReviveBeast = 77,
        [RecastGroup("Call Beast", "Call Beast", true)]
        CallBeast = 78,
        [RecastGroup("Diseased Touch", "Dis. Touch", true)]
        DiseasedTouch = 79,
        [RecastGroup("Clip", "Clip", true)]
        Clip = 80,
        [RecastGroup("Spinning Claw", "Spin. Claw", true)]
        SpinningClaw = 81,
        [RecastGroup("Flame Breath", "Flm. Breath", true)]
        FlameBreath = 82,
        [RecastGroup("Shocking Slash", "Shock. Slash", true)]
        ShockingSlash = 83,
        [RecastGroup("Bolster Armor", "Bolst. Arm.", true)]
        BolsterArmor = 84,
        [RecastGroup("Anger", "Anger", true)]
        Anger = 85,
        [RecastGroup("AOE Anger", "AOE Anger", true)]
        AOEAnger = 86,
        [RecastGroup("Claw", "Claw", true)]
        Claw = 87,
        [RecastGroup("Bolster Attack", "Bolst. Atk.", true)]
        BolsterAttack = 88,
        [RecastGroup("Hasten", "Hasten", true)]
        Hasten = 89,
        [RecastGroup("Poison Breath", "Poison Breath", true)]
        PoisonBreath = 90,
        [RecastGroup("Ice Breath", "Ice Breath", true)]
        IceBreath = 91,
        [RecastGroup("Evasive Manuever", "Eva. Maneuver", true)]
        EvasiveManeuver = 92,
        [RecastGroup("Assault", "Assault", true)]
        Assault = 93,
        [RecastGroup("Force Touch", "Force Touch", true)]
        ForceTouch = 94,
        [RecastGroup("Innervate", "Innervate", true)]
        Innervate = 95,
        [RecastGroup("Force Restore", "F. Restore", true)]
        ForceRestore = 96,
        [RecastGroup("Adrenal Stim", "Adr. Stim", true)]
        AdrenalStim = 97,
    }

    public class RecastGroupAttribute: Attribute
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public bool IsVisible { get; set; }

        public RecastGroupAttribute(string name, string shortName, bool isVisible)
        {
            Name = name;
            ShortName = shortName;
            IsVisible = isVisible;
        }
    }
}
