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
        [RecastGroup("Hacking Blade", "Hacking Blade", true)]
        HackingBlade = 14,
        [RecastGroup("Riot Blade", "Riot Blade", true)]
        RiotBlade = 15,
        [RecastGroup("Poison Stab", "Poison Stab", true)]
        PoisonStab = 16,
        [RecastGroup("Backstab", "Backstab", true)]
        Backstab = 17,        
        [RecastGroup("Force Leap", "Force Leap", true)]
        ForceLeap = 18,
        [RecastGroup("Saber Strike", "Saber Strike", true)]
        SaberStrike = 19,
        [RecastGroup("Crescent Moon", "Cresc. Moon", true)]
        CrescentMoon = 20,
        [RecastGroup("Hard Slash", "Hard Slash", true)]
        HardSlash = 21,
        [RecastGroup("Skewer", "Skewer", true)]
        Skewer = 22,
        [RecastGroup("Double Thrust", "Double Thrust", true)]
        DoubleThrust = 23,
        [RecastGroup("Leg Sweep", "Leg Sweep", true)]
        LegSweep = 24,
        [RecastGroup("Cross Cut", "Cross Cut", true)]
        CrossCut = 25,
        [RecastGroup("Circle Slash", "Circle Slash", true)]
        CircleSlash = 26,
        [RecastGroup("Double Strike", "Double Strike", true)]
        DoubleStrike = 27,
        [RecastGroup("Electric Fist", "Electric Fist", true)]
        ElectricFist = 28,
        [RecastGroup("Striking Cobra", "Striking Cobra", true)]
        StrikingCobra = 29,
        [RecastGroup("Slam", "Slam", true)]
        Slam = 30,
        [RecastGroup("Spinning Whirl", "Spinning Whirl", true)]
        SpinningWhirl = 31,
        [RecastGroup("Quick Draw", "Quick Draw", true)]
        QuickDraw = 32,
        [RecastGroup("Double Shot", "Double Shot", true)]
        DoubleShot = 33,
        [RecastGroup("Explosive Toss", "Explosive Toss", true)]
        ExplosiveToss = 34,
        [RecastGroup("Piercing Toss", "Piercing Toss", true)]
        PiercingToss = 35,
        [RecastGroup("Full Auto", "Full Auto", true)]
        FullAuto = 36,
        [RecastGroup("Hammer Shot", "Hammer Shot", true)]
        HammerShot = 37,
        [RecastGroup("Tranquilizer Shot", "Tranq. Shot", true)]
        TranquilizerShot = 38,
        [RecastGroup("Crippling Shot", "Crippling Shot", true)]
        CripplingShot = 39,
        [RecastGroup("Frag Grenade", "Frag Grenade", true)]
        FragGrenade = 40,
        [RecastGroup("Rest", "Rest", true)]
        Rest = 41,
        [RecastGroup("Knockdown", "Knockdown", true)]
        Knockdown = 42,
        [RecastGroup("Roar", "Roar", true)]
        Roar = 43,
        [RecastGroup("Bite", "Bite", true)]
        Bite = 44,
        [RecastGroup("Iron Shell", "Iron Shell", true)]
        IronShell = 45,
        [RecastGroup("Earthquake", "Earthquake", true)]
        Earthquake = 46,
        [RecastGroup("Fire Breath", "F. Breath", true)]
        FireBreath = 47,
        [RecastGroup("Spikes", "Spikes", true)]
        Spikes = 48,
        [RecastGroup("Venom", "Venom", true)]
        Venom = 49,
        [RecastGroup("Talon", "Talon", true)]
        Talon = 50,
        [RecastGroup("Med Kit", "Med Kit", true)]
        MedKit = 51,
        [RecastGroup("Kolto Recovery", "K. Recovery", true)]
        KoltoRecovery = 52,
        [RecastGroup("Resuscitation", "Resuscitation", true)]
        Resuscitation = 53,
        [RecastGroup("Treatment Kit", "Treatment Kit", true)]
        TreatmentKit = 54,
        [RecastGroup("Stasis Field", "Stasis Field", true)]
        StasisField = 55,
        [RecastGroup("Combat Enhancement", "Combat En.", true)]
        CombatEnhancement = 56,
        [RecastGroup("Shielding", "Shielding", true)]
        Shielding = 57,
        [RecastGroup("Chi", "Chi", true)]
        Chi = 58,
        [RecastGroup("Bombs", "Bombs", true)]
        Bombs = 59,
        [RecastGroup("Stealth Generator", "Stealth Gen.", true)]
        StealthGenerator = 60,
        [RecastGroup("Flamethrower", "Flamethrower", true)]
        Flamethrower = 61,
        [RecastGroup("Wrist Rocket", "W. Rocket", true)]
        WristRocket = 62,
        [RecastGroup("Deflector Shield", "D. Shield", true)]
        DeflectorShield = 63,
        [RecastGroup("Provoke", "Provoke", true)]
        Provoke = 64,
        [RecastGroup("Provoke II", "Provoke II", true)]
        Provoke2 = 65,
        [RecastGroup("Premonition", "Premonition", true)]
        Premonition = 66,
        [RecastGroup("Disturbance", "Disturbance", true)]
        Disturbance = 67,
        [RecastGroup("Benevolence", "Benevolence", true)]
        Benevolence = 68,
        [RecastGroup("Force Valor", "F. Valor", true)]
        ForceValor = 69,
        [RecastGroup("Force Spark", "F. Spark", true)]
        ForceSpark = 70,
        [RecastGroup("Creeping Terror", "C. Terror", true)]
        CreepingTerror = 71,
        [RecastGroup("Force Rage", "F. Rage", true)]
        ForceRage = 72,
        [RecastGroup("Furor", "Furor", true)]
        Furor = 73,
        [RecastGroup("Throw Rock", "Throw Rock", true)]
        ThrowRock = 74,
        [RecastGroup("Force Inspiration", "F. Inspiration", true)]
        ForceInspiration = 75,
        [RecastGroup("Shield Bash", "Shield Bash", true)]
        ShieldBash = 76,
        [RecastGroup("Rousing Shout", "R. Shout", true)]
        RousingShout = 77,
        [RecastGroup("Dedication", "Dedication", true)]
        Dedication = 78,
        [RecastGroup("Soldier's Speed", "Sol. Speed", true)]
        SoldiersSpeed = 79,
        [RecastGroup("Soldier's Strike", "Sol. Strike", true)]
        SoldiersStrike = 80,
        [RecastGroup("Charge", "Charge", true)]
        Charge = 81,
        [RecastGroup("Soldier's Precisionn", "Sol. Precision", true)]
        SoldiersPrecision = 82,
        [RecastGroup("Shocking Shout", "Shock. Shout", true)]
        ShockingShout = 83,
        [RecastGroup("Rejuvenation", "Rejuvenation", true)]
        Rejuvenation = 84,
        [RecastGroup("Frenzied Shout", "Frenz. Shout", true)]
        FrenziedShout = 85,
        [RecastGroup("Screech", "Screech", true)]
        Screech = 86,
        [RecastGroup("Flame Blast", "F. Blast", true)]
        FlameBlast = 87,
        [RecastGroup("Greater Earthquake", "G. Quake", true)]
        GreaterEarthquake = 88,
        [RecastGroup("Infusion", "Infusion", true)]
        Infusion = 89,
        [RecastGroup("Droid Controller", "Droid Contr.", true)]
        DroidController = 90,
        [RecastGroup("Stat Rebuild", "Stat Rebuild", false)]
        StatRebuild = 91,
        [RecastGroup("Concussion Grenade", "Conc. Grenade", true)]
        ConcussionGrenade = 92,
        [RecastGroup("Flashbang Grenade", "Flash. Grenade", true)]
        FlashbangGrenade = 93,
        [RecastGroup("Ion Grenade", "Ion Grenade", true)]
        IonGrenade = 94,
        [RecastGroup("Kolto Grenade", "Kolto Grenade", true)]
        KoltoGrenade = 95,
        [RecastGroup("Adhesive Grenade", "Adhe. Grenade", true)]
        AdhesiveGrenade = 96,
        [RecastGroup("Smoke Bomb", "Smoke Bomb", true)]
        SmokeBomb = 97,
        [RecastGroup("Kolto Bomb", "Kolto Bomb", true)]
        KoltoBomb = 98,
        [RecastGroup("Incendiary Bomb", "Incen. Bomb", true)]
        IncendiaryBomb = 99,
        [RecastGroup("Gas Bomb", "Gas Bomb", true)]
        GasBomb = 100,
        [RecastGroup("Tame", "Tame", true)]
        Tame = 101,
        [RecastGroup("Reward", "Reward", true)]
        Reward = 102,
        [RecastGroup("Snarl/Growl", "Snarl/Growl", true)]
        SnarlGrowl = 103,
        [RecastGroup("Soothe Pet", "Soothe Pet", true)]
        SoothePet = 104,
        [RecastGroup("Revive Beast", "Rev. Beast", true)]
        ReviveBeast = 105,
        [RecastGroup("Call Beast", "Call Beast", true)]
        CallBeast = 106,
        [RecastGroup("Diseased Touch", "Dis. Touch", true)]
        DiseasedTouch = 107,
        [RecastGroup("Clip", "Clip", true)]
        Clip = 108,
        [RecastGroup("Spinning Claw", "Spin. Claw", true)]
        SpinningClaw = 109,
        [RecastGroup("Flame Breath", "Flm. Breath", true)]
        FlameBreath = 110,
        [RecastGroup("Shocking Slash", "Shock. Slash", true)]
        ShockingSlash = 111,
        [RecastGroup("Bolster Armor", "Bolst. Arm.", true)]
        BolsterArmor = 112,
        [RecastGroup("Anger", "Anger", true)]
        Anger = 113,
        [RecastGroup("AOE Anger", "AOE Anger", true)]
        AOEAnger = 114,
        [RecastGroup("Claw", "Claw", true)]
        Claw = 115,
        [RecastGroup("Bolster Attack", "Bolst. Atk.", true)]
        BolsterAttack = 116,
        [RecastGroup("Hasten", "Hasten", true)]
        Hasten = 117,
        [RecastGroup("Poison Breath", "Poison Breath", true)]
        PoisonBreath = 118,
        [RecastGroup("Ice Breath", "Ice Breath", true)]
        IceBreath = 119,
        [RecastGroup("Evasive Manuever", "Eva. Maneuver", true)]
        EvasiveManeuver = 120,
        [RecastGroup("Assault", "Assault", true)]
        Assault = 121,
        [RecastGroup("Force Touch", "Force Touch", true)]
        ForceTouch = 122,
        [RecastGroup("Innervate", "Innervate", true)]
        Innervate = 123,
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
