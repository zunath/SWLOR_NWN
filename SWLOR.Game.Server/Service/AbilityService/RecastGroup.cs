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
        [RecastGroup("Soldier's Precision", "Sol. Precision", true)]
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
        [RecastGroup("Evasive Maneuver", "Eva. Maneuver", true)]
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
        [RecastGroup("Carve", "Carve", true)]
        Carve = 98,
        [RecastGroup("Covering Strike", "Cover Strike", true)]
        CoveringStrike = 99,
        [RecastGroup("Rending Strike", "Rend Strike", true)]
        RendingStrike = 100,
        [RecastGroup("Savage Cleave", "Savage Cleave", true)]
        SavageCleave = 101,
        [RecastGroup("Whirlwind Assault", "Whirlwind", true)]
        WhirlwindAssault = 102,
        [RecastGroup("Soul Burst", "Soul Burst", true)]
        SoulBurst = 103,
        [RecastGroup("Crushing Blow", "Crushing Blow", true)]
        CrushingBlow = 104,
        [RecastGroup("Edge of Darkness", "E. Darkness", true)]
        EdgeOfDarkness = 105,
        [RecastGroup("Sacrificial Blade", "Sacrif. Blade", true)]
        SacrificialBlade = 106,
        [RecastGroup("Iron Elbows", "Iron Elbows", true)]
        IronElbows = 107,
        [RecastGroup("Guard Counter", "Guard Counter", true)]
        GuardCounter = 108,
        [RecastGroup("Covering Claws", "Covering Claws", true)]
        CoveringClaws = 109,
        [RecastGroup("Whirling Guard", "Whirling Guard", true)]
        WhirlingGuard = 110,
        [RecastGroup("Breaker Reversal", "Breaker Rev.", true)]
        BreakerReversal = 111,
        [RecastGroup("Static Palm", "Static Palm", true)]
        StaticPalm = 112,
        [RecastGroup("Twin Fang Flurry", "Twin Flurry", true)]
        TwinFangFlurry = 113,
        [RecastGroup("Venom Splash", "Venom Splash", true)]
        VenomSplash = 114,
        [RecastGroup("Neural Shock", "Neural Shock", true)]
        NeuralShock = 115,
        [RecastGroup("Current Overload", "Curr. Overload", true)]
        CurrentOverload = 116,
        [RecastGroup("Serpent's Eclipse", "Serp. Eclipse", true)]
        SerpentsEclipse = 117,
        [RecastGroup("Punishing Strike", "Punish Strike", true)]
        PunishingStrike = 118,
        [RecastGroup("Guardian's Challenge", "Guard. Chall.", true)]
        GuardiansChallenge = 119,
        [RecastGroup("Versatile Strike", "Vers. Strike", true)]
        VersatileStrike = 120,
        [RecastGroup("Leg Slash", "Leg Slash", true)]
        LegSlash = 121,
        [RecastGroup("Ripple Slash", "Ripple Slash", true)]
        RippleSlash = 122,
        [RecastGroup("Fan the Hammer", "Fan the Hammer", true)]
        FanTheHammer = 123,
        [RecastGroup("Dead Man's Hand", "Dead Man Hand", true)]
        DeadMansHand = 124,
        [RecastGroup("Disarming Shot", "Disarming Shot", true)]
        DisarmingShot = 125,
        [RecastGroup("Ricochet Shot", "Ricochet Shot", true)]
        RicochetShot = 126,
        [RecastGroup("Low Shot", "Low Shot", true)]
        LowShot = 127,
        [RecastGroup("Point Blank Burst", "P. Blank Burst", true)]
        PointBlankBurst = 128,
        [RecastGroup("Smoke Round", "Smoke Round", true)]
        SmokeRound = 129,
        [RecastGroup("Last Word", "Last Word", true)]
        LastWord = 130,
        [RecastGroup("Aimed Shot", "Aimed Shot", true)]
        AimedShot = 131,
        [RecastGroup("Piercing Round", "Piercing Round", true)]
        PiercingRound = 132,
        [RecastGroup("Suppressive Line", "Suppress Line", true)]
        SuppressiveLine = 133,
        [RecastGroup("Expose Weak Point", "Expose W. Pt.", true)]
        ExposeWeakPoint = 134,
        [RecastGroup("Breach Round", "Breach Round", true)]
        BreachRound = 135,
        [RecastGroup("Headshot", "Headshot", true)]
        Headshot = 136,
        [RecastGroup("One Shot", "One Shot", true)]
        OneShot = 137,
        [RecastGroup("Pinning Fire", "Pinning Fire", true)]
        PinningFire = 138,
        [RecastGroup("Overwatch", "Overwatch", true)]
        Overwatch = 139,
        [RecastGroup("Neutralizing Shot", "Neutral Shot", true)]
        NeutralizingShot = 140,
        [RecastGroup("Pacification Field", "Pacify Field", true)]
        PacificationField = 141,
        [RecastGroup("Stasis Volley", "Stasis Volley", true)]
        StasisVolley = 142,
        [RecastGroup("Maelstrom Arc", "Maelstrom Arc", true)]
        MaelstromArc = 143,
        [RecastGroup("Force Gyre", "Force Gyre", true)]
        ForceGyre = 144,
        [RecastGroup("Tempest Release", "Temp. Release", true)]
        TempestRelease = 145,
        [RecastGroup("Saber Cyclone", "Saber Cyclone", true)]
        SaberCyclone = 146,
        [RecastGroup("Focused Arc", "Focused Arc", true)]
        FocusedArc = 147,
        [RecastGroup("Sever Focus", "Sever Focus", true)]
        SeverFocus = 148,
        [RecastGroup("Conduit Flare", "Conduit Flare", true)]
        ConduitFlare = 149,
        [RecastGroup("Force Suppression", "F. Suppress", true)]
        ForceSuppression = 150,
        [RecastGroup("Force Nullification", "F. Nullify", true)]
        ForceNullification = 151,
        [RecastGroup("Total Force Denial", "Force Denial", true)]
        TotalForceDenial = 152,
        [RecastGroup("Breach Strike", "Breach Strike", true)]
        BreachStrike = 153,
        [RecastGroup("Flanking Barrage", "Flank Barrage", true)]
        FlankingBarrage = 154,
        [RecastGroup("Sweeping Flank", "Sweeping Flank", true)]
        SweepingFlank = 155,
        [RecastGroup("Hampering Barrage", "Hamper Barr.", true)]
        HamperingBarrage = 156,
        [RecastGroup("Line Breaker", "Line Breaker", true)]
        LineBreaker = 157,
        [RecastGroup("Sweeping Guard", "Sweeping Guard", true)]
        SweepingGuard = 158,
        [RecastGroup("Rib Breaker", "Rib Breaker", true)]
        RibBreaker = 159,
        [RecastGroup("Ground Quake", "Ground Quake", true)]
        GroundQuake = 160,
        [RecastGroup("Skull Rattle", "Skull Rattle", true)]
        SkullRattle = 161,
        [RecastGroup("Bonecrusher", "Bonecrusher", true)]
        Bonecrusher = 162,
        [RecastGroup("Worldbreaker", "Worldbreaker", true)]
        Worldbreaker = 163,
        [RecastGroup("Concussive Toss", "Concuss Toss", true)]
        ConcussiveToss = 164,
        [RecastGroup("Fireburst Toss", "Fireburst Toss", true)]
        FireburstToss = 165,
        [RecastGroup("Cluster Storm", "Cluster Storm", true)]
        ClusterStorm = 166,
        [RecastGroup("Flash Toss", "Flash Toss", true)]
        FlashToss = 167,
        [RecastGroup("Saturation Toss", "Satur. Toss", true)]
        SaturationToss = 168,
        [RecastGroup("Rain of Steel", "Rain of Steel", true)]
        RainOfSteel = 169,
        [RecastGroup("Pinning Toss", "Pinning Toss", true)]
        PinningToss = 170,
        [RecastGroup("Marking Toss", "Marking Toss", true)]
        MarkingToss = 171,
        [RecastGroup("Ricochet Toss", "Ricochet Toss", true)]
        RicochetToss = 172,
        [RecastGroup("Severing Toss", "Severing Toss", true)]
        SeveringToss = 173,
        [RecastGroup("Finishing Toss", "Finishing Toss", true)]
        FinishingToss = 174,
        [RecastGroup("Perfect Throw", "Perfect Throw", true)]
        PerfectThrow = 175,
        [RecastGroup("Blade Vortex", "Blade Vortex", true)]
        BladeVortex = 176,
        [RecastGroup("Sweeping Advance", "Sweep Advance", true)]
        SweepingAdvance = 177,
        [RecastGroup("Storm Release", "Storm Release", true)]
        StormRelease = 178,
        [RecastGroup("Tempest Bloom", "Tempest Bloom", true)]
        TempestBloom = 179,
        [RecastGroup("Split Guard Strike", "Split Strike", true)]
        SplitGuardStrike = 180,
        [RecastGroup("Feinting Cut", "Feinting Cut", true)]
        FeintingCut = 181,
        [RecastGroup("Binding Cross", "Binding Cross", true)]
        BindingCross = 182,
        [RecastGroup("Reversal Cut", "Reversal Cut", true)]
        ReversalCut = 183,
        [RecastGroup("Cheap Shot", "Cheap Shot", true)]
        CheapShot = 184,
        [RecastGroup("Shadow Strike", "Shadow Strike", true)]
        ShadowStrike = 185,
        [RecastGroup("Vital Strike", "Vital Strike", true)]
        VitalStrike = 186,
        [RecastGroup("Enfeebling Strike", "Enfeeble Str.", true)]
        EnfeeblingStrike = 187,
        [RecastGroup("Sap Vitality", "Sap Vitality", true)]
        SapVitality = 188,
        [RecastGroup("Nerve Strike", "Nerve Strike", true)]
        NerveStrike = 189,
        [RecastGroup("Cascade Failure", "C. Failure", true)]
        CascadeFailure = 190,
        [RecastGroup("Systemic Shutdown", "Sys. Shutdown", true)]
        SystemicShutdown = 191,
        [RecastGroup("Soul Strike", "Soul Strike", true)]
        SoulStrike = 192,
        [RecastGroup("Essence Hunter", "Essence Hunt", true)]
        EssenceHunter = 193,
        [RecastGroup("Soul Devourer", "Soul Devour", true)]
        SoulDevourer = 194,
        [RecastGroup("Soul Sacrifice", "Soul Sacrifice", true)]
        SoulSacrifice = 195,
        [RecastGroup("Soul Storm", "Soul Storm", true)]
        SoulStorm = 196,
        [RecastGroup("Soul Ascension", "Soul Ascens.", true)]
        SoulAscension = 197,
        [RecastGroup("Anger Strike", "Anger Strike", true)]
        AngerStrike = 198,
        [RecastGroup("Bastion Stance", "Bastion St.", true)]
        BastionStance = 199,
        [RecastGroup("Guardian's Resolve", "Guardian Res", true)]
        GuardiansResolve = 200,
        [RecastGroup("Rampart", "Rampart", true)]
        Rampart = 201,
        [RecastGroup("Absolute Defense", "Abs. Defense", true)]
        AbsoluteDefense = 202,
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
