using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    // Note: Short names are what's displayed on the recast Gui element. They are limited to 14 characters.
    public enum RecastGroup
    {
        [RecastGroup("Invalid", "Invalid")]
        Invalid = 0,
        [RecastGroup("Burst Of Speed", "Burst Of Speed")]
        BurstOfSpeed = 1,
        [RecastGroup("Force Heal", "Force Heal")]
        ForceHeal = 2,
        [RecastGroup("Force Push", "Force Push")]
        ForcePush = 3,
        [RecastGroup("Throw Lightsaber", "Throw Saber")]
        ThrowLightsaber = 4,
        [RecastGroup("Force Stun", "Force Stun")]
        ForceStun = 5,
        [RecastGroup("Battle Insight", "Battle Insight")]
        BattleInsight = 6,
        [RecastGroup("Comprehend Speech", "Comp. Speech")]
        ComprehendSpeech = 7,
        [RecastGroup("Mind Trick", "Mind Trick")]
        MindTrick = 8,
        [RecastGroup("Force Breach", "Force Breach")]
        ForceBreach = 9,
        [RecastGroup("Force Body", "Force Body")]
        ForceBody = 10,
        [RecastGroup("Force Drain", "Force Drain")]
        ForceDrain = 11,
        [RecastGroup("Force Lightning", "F. Lightning")]
        ForceLightning = 12,
        [RecastGroup("Force Mind", "Force Mind")]
        ForceMind = 13,
        [RecastGroup("Hacking Blade", "Hacking Blade")]
        HackingBlade = 14,
        [RecastGroup("Riot Blade", "Riot Blade")]
        RiotBlade = 15,
        [RecastGroup("Poison Stab", "Poison Stab")]
        PoisonStab = 16,
        [RecastGroup("Backstab", "Backstab")]
        Backstab = 17,        
        [RecastGroup("Force Leap", "Force Leap")]
        ForceLeap = 18,
        [RecastGroup("Saber Strike", "Saber Strike")]
        SaberStrike = 19,
        [RecastGroup("Crescent Moon", "Cresc. Moon")]
        CrescentMoon = 20,
        [RecastGroup("Hard Slash", "Hard Slash")]
        HardSlash = 21,
        [RecastGroup("Skewer", "Skewer")]
        Skewer = 22,
        [RecastGroup("Double Thrust", "Double Thrust")]
        DoubleThrust = 23,
        [RecastGroup("Leg Sweep", "Leg Sweep")]
        LegSweep = 24,
        [RecastGroup("Cross Cut", "Cross Cut")]
        CrossCut = 25,
        [RecastGroup("Circle Slash", "Circle Slash")]
        CircleSlash = 26,
        [RecastGroup("Double Strike", "Double Strike")]
        DoubleStrike = 27,
        [RecastGroup("Electric Fist", "Electric Fist")]
        ElectricFist = 28,
        [RecastGroup("Striking Cobra", "Striking Cobra")]
        StrikingCobra = 29,
        [RecastGroup("Slam", "Slam")]
        Slam = 30,
        [RecastGroup("Spinning Whirl", "Spinning Whirl")]
        SpinningWhirl = 31,
        [RecastGroup("Quick Draw", "Quick Draw")]
        QuickDraw = 32,
        [RecastGroup("Double Shot", "Double Shot")]
        DoubleShot = 33,
        [RecastGroup("Explosive Toss", "Explosive Toss")]
        ExplosiveToss = 34,
        [RecastGroup("Piercing Toss", "Piercing Toss")]
        PiercingToss = 35,
        [RecastGroup("Full Auto", "Full Auto")]
        FullAuto = 36,
        [RecastGroup("Hammer Shot", "Hammer Shot")]
        HammerShot = 37,
        [RecastGroup("Tranquilizer Shot", "Tranq. Shot")]
        TranquilizerShot = 38,
        [RecastGroup("Crippling Shot", "Crippling Shot")]
        CripplingShot = 39,
        [RecastGroup("Grenades", "Grenades")]
        Grenades = 40,
        [RecastGroup("Rest", "Rest")]
        Rest = 41,
        [RecastGroup("Knockdown", "Knockdown")]
        Knockdown = 42,
        [RecastGroup("Implant Installation", "Imp. Install")]
        ImplantInstallation = 43,
    }

    public class RecastGroupAttribute: Attribute
    {
        public string Name { get; set; }
        public string ShortName { get; set; }

        public RecastGroupAttribute(string name, string shortName)
        {
            Name = name;
            ShortName = shortName;
        }
    }
}
