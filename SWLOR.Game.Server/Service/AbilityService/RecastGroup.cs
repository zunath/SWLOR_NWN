namespace SWLOR.Game.Server.Service.AbilityService
{
    // Note: Short names are what's displayed on the recast Gui element. They are limited to 14 characters.
    public enum RecastGroup
    {
        [RecastGroup("Invalid", "Invalid", false)]
        Invalid = 0,
        [RecastGroup("Bloodlust", "Bloodlust", true)]
        Bloodlust = 1,
        [RecastGroup("Blood Weapon", "Blood Weapon", true)]
        BloodWeapon = 2,
        [RecastGroup("Earthshatter", "Earthshatter", true)]
        Earthshatter = 3,
        [RecastGroup("Flash", "Flash", true)]
        Flash = 4,
        [RecastGroup("Fortress Strike", "Fortress Str.", true)]
        FortressStrike = 5,
        [RecastGroup("Arc Strike", "Arc Strike", true)]
        ArcStrike = 6,
        [RecastGroup("Overwhelming Strike", "Overwhelm Str.", true)]
        OverwhelmingStrike = 7,
        [RecastGroup("Interruption Strike", "Interrupt Str.", true)]
        InterruptionStrike = 8,
        [RecastGroup("Fracture Strike", "Fracture Str.", true)]
        FractureStrike = 9,
        [RecastGroup("Saber Storm", "Saber Storm", true)]
        SaberStorm = 10,
        [RecastGroup("Thunderous Challenge", "Thunder Chal.", true)]
        ThunderousChallenge = 11,
        [RecastGroup("Rest", "Rest", true)]
        Rest = 16,
        [RecastGroup("Mauling Bite", "Maul Bite", true)]
        MaulingBite = 17,
        [RecastGroup("Bite", "Bite", true)]
        Bite = 18,
        [RecastGroup("Bonecrusher Bite", "Bone Bite", true)]
        BonecrusherBite = 19,
        [RecastGroup("Raking Claws", "Rake Claws", true)]
        RakingClaws = 20,
        [RecastGroup("Pouncing Strike", "Pounce", true)]
        PouncingStrike = 21,
        [RecastGroup("Tail Sweep", "Tail Sweep", true)]
        TailSweep = 22,
        [RecastGroup("Goring Charge", "Gore Charge", true)]
        GoringCharge = 23,
        [RecastGroup("Barbed Volley", "Barb Volley", true)]
        BarbedVolley = 24,
        [RecastGroup("Rending Bite", "Rend Bite", true)]
        RendingBite = 25,
        [RecastGroup("Crippling Talons", "Crip Talons", true)]
        CripplingTalons = 26,
        [RecastGroup("Piercing Quills", "Quills", true)]
        PiercingQuills = 27,
        [RecastGroup("Toxic Spit", "Toxic Spit", true)]
        ToxicSpit = 28,
        [RecastGroup("Scorching Breath", "Scorch Breath", true)]
        ScorchingBreath = 29,
        [RecastGroup("Inferno Blast", "Inferno", true)]
        InfernoBlast = 30,
        [RecastGroup("Seismic Slam", "Seismic Slam", true)]
        SeismicSlam = 31,
        [RecastGroup("Rupturing Quake", "Rupt. Quake", true)]
        RupturingQuake = 32,
        [RecastGroup("Terrifying Bellow", "Bellow", true)]
        TerrifyingBellow = 33,
        [RecastGroup("Disorienting Screech", "Dis. Screech", true)]
        DisorientingScreech = 34,
        [RecastGroup("Iron Carapace", "Carapace", true)]
        IronCarapace = 35,
        [RecastGroup("Venom Spray", "Venom Spray", true)]
        VenomSpray = 36,
        [RecastGroup("Provoke", "Provoke", true)]
        Provoke = 37,
        [RecastGroup("Provoke II", "Provoke II", true)]
        Provoke2 = 38,
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
        [RecastGroup("Toxic Cloud", "Toxic Cloud", true)]
        ToxicCloud = 58,
        [RecastGroup("Frost Spit", "Frost Spit", true)]
        FrostSpit = 59,
        [RecastGroup("Static Burst", "Static Burst", true)]
        StaticBurst = 60,
        [RecastGroup("Savage Roar", "Savage Roar", true)]
        SavageRoar = 61,
        [RecastGroup("Droid Controller", "Droid Contr.", true)]
        DroidController = 62,
        [RecastGroup("Stat Rebuild", "Stat Rebuild", false)]
        StatRebuild = 63,
        [RecastGroup("Smoke Bomb", "Smoke Bomb", true)]
        SmokeBomb = 69,
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
        [RecastGroup("Sonic Shriek", "Sonic Shriek", true)]
        SonicShriek = 79,
        [RecastGroup("Chitin Guard", "Chitin Guard", true)]
        ChitinGuard = 80,
        [RecastGroup("Precision Shot", "Precision Shot", true)]
        PrecisionShot = 81,
        [RecastGroup("Suppressing Shot", "Suppress Shot", true)]
        SuppressingShot = 82,
        [RecastGroup("Grenade Burst", "Grenade", true)]
        GrenadeBurst = 83,
        [RecastGroup("Serrated Slash", "Serr. Slash", true)]
        SerratedSlash = 84,
        [RecastGroup("Brutal Bash", "Brutal Bash", true)]
        BrutalBash = 85,
        [RecastGroup("Tactical Mark", "Tac. Mark", true)]
        TacticalMark = 86,
        [RecastGroup("Overload Shot", "Overload Shot", true)]
        OverloadShot = 87,
        [RecastGroup("Arc Pulse", "Arc Pulse", true)]
        ArcPulse = 88,
        [RecastGroup("Ion Burst", "Ion Burst", true)]
        IonBurst = 89,
        [RecastGroup("Target Lock", "Target Lock", true)]
        TargetLock = 90,
        [RecastGroup("Shrapnel Burst", "Shrapnel", true)]
        ShrapnelBurst = 91,
        [RecastGroup("Force Rend", "Force Rend", true)]
        ForceRend = 92,
        [RecastGroup("Mind Spike", "Mind Spike", true)]
        MindSpike = 93,
        [RecastGroup("Dark Shock", "Dark Shock", true)]
        DarkShock = 94,
        [RecastGroup("Dread Wave", "Dread Wave", true)]
        DreadWave = 95,
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
        [RecastGroup("Dead Man's Hand", "Dead Man's", true)]
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
        [RecastGroup("Guardian's Resolve", "Guard. Resolve", true)]
        GuardiansResolve = 200,
        [RecastGroup("Rampart", "Rampart", true)]
        Rampart = 201,
        [RecastGroup("Absolute Defense", "Abs. Defense", true)]
        AbsoluteDefense = 202,
        [RecastGroup("Surge Strike", "Surge Strike", true)]
        SurgeStrike = 203,
        [RecastGroup("Quick Draw", "Quick Draw", true)]
        QuickDraw = 204,
        [RecastGroup("Snap Roll", "Snap Roll", true)]
        SnapRoll = 205,
        [RecastGroup("Gunslinger Focus", "Guns. Focus", true)]
        GunslingerFocus = 206,
        [RecastGroup("Double Shot", "Double Shot", true)]
        DoubleShot = 207,
        [RecastGroup("Twin Intercept", "Twin Intercept", true)]
        TwinIntercept = 208,
        [RecastGroup("Taunting Deflection", "Taunt Deflect", true)]
        TauntingDeflection = 209,
        [RecastGroup("Second Wind", "Second Wind", true)]
        SecondWind = 210,
        [RecastGroup("Purify", "Purify", true)]
        Purify = 211,
        [RecastGroup("Adamantine Guard", "Adamant Guard", true)]
        AdamantineGuard = 212,
        [RecastGroup("Cobra Stance", "Cobra Stance", true)]
        CobraStance = 213,
        [RecastGroup("Iron Wall Stance", "Iron Wall", true)]
        IronWallStance = 214,
        [RecastGroup("Striking Cobra", "Striking Cobra", true)]
        StrikingCobra = 215,
        [RecastGroup("Toxic Rush", "Toxic Rush", true)]
        ToxicRush = 216,
        [RecastGroup("Twin Guard Stance", "Twin Guard", true)]
        TwinGuardStance = 217,
        [RecastGroup("Brutal Assault", "Brutal Assault", true)]
        BrutalAssault = 218,
        [RecastGroup("Ferocity Stance", "Ferocity St.", true)]
        FerocityStance = 219,
        [RecastGroup("Focused Stance", "Focused Stance", true)]
        FocusedStance = 220,
        [RecastGroup("Guardian Master", "Guard. Master", true)]
        GuardianMaster = 221,
        [RecastGroup("Guardian's Influence", "Guard. Influ.", true)]
        GuardiansInfluence = 222,
        [RecastGroup("Impenetrable Guard", "Impen. Guard", true)]
        ImpenetrableGuard = 223,
        [RecastGroup("Gunfighter Stance", "Gunfighter St.", true)]
        GunfighterStance = 224,
        [RecastGroup("Interrupting Shot", "Interrupt Shot", true)]
        InterruptingShot = 225,
        [RecastGroup("Skirmisher Stance", "Skirmisher St.", true)]
        SkirmisherStance = 226,
        [RecastGroup("Crippling Shot", "Crippling Shot", true)]
        CripplingShot = 227,
        [RecastGroup("Kill Zone", "Kill Zone", true)]
        KillZone = 228,
        [RecastGroup("Sniper Stance", "Sniper Stance", true)]
        SniperStance = 229,
        [RecastGroup("Spotter Stance", "Spotter Stance", true)]
        SpotterStance = 230,
        [RecastGroup("Tranq Cone", "Tranq Cone", true)]
        TranqCone = 231,
        [RecastGroup("Tranquilizer Shot", "Tranq Shot", true)]
        TranquilizerShot = 232,
        [RecastGroup("Circle Slash", "Circle Slash", true)]
        CircleSlash = 233,
        [RecastGroup("Conduit Stance", "Conduit Stance", true)]
        ConduitStance = 234,
        [RecastGroup("Double Strike", "Double Strike", true)]
        DoubleStrike = 235,
        [RecastGroup("Force Capacitor", "Force Cap.", true)]
        ForceCapacitor = 236,
        [RecastGroup("Force Lens", "Force Lens", true)]
        ForceLens = 237,
        [RecastGroup("Guarded Channel", "Guard Channel", true)]
        GuardedChannel = 238,
        [RecastGroup("Infinite Conduit", "Infin. Conduit", true)]
        InfiniteConduit = 239,
        [RecastGroup("Tempest Stance", "Tempest Stance", true)]
        TempestStance = 240,
        [RecastGroup("Calming Stance", "Calming Stance", true)]
        CalmingStance = 241,
        [RecastGroup("Crippling Defense", "Crip. Def.", true)]
        CripplingDefense = 242,
        [RecastGroup("Disabling Strike", "Disable Strike", true)]
        DisablingStrike = 243,
        [RecastGroup("Disruption Field", "Disrupt Field", true)]
        DisruptionField = 244,
        [RecastGroup("Flanking Stance", "Flank Stance", true)]
        FlankingStance = 245,
        [RecastGroup("Forcebane", "Forcebane", true)]
        Forcebane = 246,
        [RecastGroup("Improved Attentiveness", "Impr. Attent.", true)]
        ImprovedAttentiveness = 247,
        [RecastGroup("Perceptive Stance", "Percept. St.", true)]
        PerceptiveStance = 248,
        [RecastGroup("Side Assault", "Side Assault", true)]
        SideAssault = 249,
        [RecastGroup("Crusher Stance", "Crusher Stance", true)]
        CrusherStance = 250,
        [RecastGroup("Guarding Step", "Guarding Step", true)]
        GuardingStep = 251,
        [RecastGroup("Leg Sweep", "Leg Sweep", true)]
        LegSweep = 252,
        [RecastGroup("Sentinel Guard", "Sentinel Guard", true)]
        SentinelGuard = 253,
        [RecastGroup("Sentinel Stance", "Sentinel St.", true)]
        SentinelStance = 254,
        [RecastGroup("Shelter Circle", "Shelter Circle", true)]
        ShelterCircle = 255,
        [RecastGroup("Slam", "Slam", true)]
        Slam = 256,
        [RecastGroup("Unmoving Center", "Unmoving Ctr.", true)]
        UnmovingCenter = 257,
        [RecastGroup("Bombardier Stance", "Bombardier St.", true)]
        BombardierStance = 258,
        [RecastGroup("Deadeye Stance", "Deadeye Stance", true)]
        DeadeyeStance = 259,
        [RecastGroup("Explosive Toss", "Explosive Toss", true)]
        ExplosiveToss = 260,
        [RecastGroup("Piercing Toss", "Piercing Toss", true)]
        PiercingToss = 261,
        [RecastGroup("Cross Cut", "Cross Cut", true)]
        CrossCut = 262,
        [RecastGroup("Cyclone Stance", "Cyclone Stance", true)]
        CycloneStance = 263,
        [RecastGroup("Duelist Stance", "Duelist Stance", true)]
        DuelistStance = 264,
        [RecastGroup("Duelist's Challenge", "Duelist Chal.", true)]
        DuelistsChallenge = 265,
        [RecastGroup("Final Form", "Final Form", true)]
        FinalForm = 266,
        [RecastGroup("Spinning Whirl", "Spinning Whirl", true)]
        SpinningWhirl = 267,
        [RecastGroup("Berserker Stance", "Berserker St.", true)]
        BerserkerStance = 268,
        [RecastGroup("Defensive Stance", "Defensive St.", true)]
        DefensiveStance = 269,
        [RecastGroup("Hacking Blade", "Hacking Blade", true)]
        HackingBlade = 270,
        [RecastGroup("Invincible", "Invincible", true)]
        Invincible = 271,
        [RecastGroup("Riot Blade", "Riot Blade", true)]
        RiotBlade = 272,
        [RecastGroup("Shield Bash", "Shield Bash", true)]
        ShieldBash = 273,
        [RecastGroup("Shield Wall", "Shield Wall", true)]
        ShieldWall = 274,
        [RecastGroup("Backstab", "Backstab", true)]
        Backstab = 275,
        [RecastGroup("Deadly Precision", "Deadly Prec.", true)]
        DeadlyPrecision = 276,
        [RecastGroup("Debilitating Stance", "Debil. Stance", true)]
        DebilitatingStance = 277,
        [RecastGroup("Decoy", "Decoy", true)]
        Decoy = 278,
        [RecastGroup("Evasive Combat", "Evasive Combat", true)]
        EvasiveCombat = 279,
        [RecastGroup("Hamstring", "Hamstring", true)]
        Hamstring = 280,
        [RecastGroup("Incapacitate", "Incapacitate", true)]
        Incapacitate = 281,
        [RecastGroup("Marked for Death", "Marked Death", true)]
        MarkedForDeath = 282,
        [RecastGroup("Toxic Coating", "Toxic Coating", true)]
        ToxicCoating = 283,
        [RecastGroup("Centering", "Centering", true)]
        Centering = 284,
        [RecastGroup("Adhesive Grenade", "Adhesive Gren", true)]
        AdhesiveGrenade = 285,
        [RecastGroup("Adrenal Stim", "Adrenal Stim", true)]
        AdrenalStim = 286,
        [RecastGroup("Alpha Rhythm", "Alpha Rhythm", true)]
        AlphaRhythm = 287,
        [RecastGroup("Anger", "Anger", true)]
        Anger = 288,
        [RecastGroup("Antitoxin", "Antitoxin", true)]
        Antitoxin = 289,
        [RecastGroup("Apex Bite", "Apex Bite", true)]
        ApexBite = 290,
        [RecastGroup("Assault", "Assault", true)]
        Assault = 291,
        [RecastGroup("Courageous Resolve", "Courage Res.", true)]
        AuraOfCourage = 292,
        [RecastGroup("Bastion of Light", "Light Bastion", true)]
        BastionOfLight = 293,
        [RecastGroup("Benevolence", "Benevolence", true)]
        Benevolence = 294,
        [RecastGroup("Blaster Beacon", "Blaster Beacon", true)]
        BlasterBeacon = 295,
        [RecastGroup("Bolster Attack", "Bolster Attack", true)]
        BolsterAttack = 296,
        [RecastGroup("Bolster Resolve", "Bolster Res.", true)]
        BolsterResolve = 297,
        [RecastGroup("Break Morale", "Break Morale", true)]
        BreakMorale = 298,
        [RecastGroup("Circle of Harmony", "Harmony Circle", true)]
        CircleOfHarmony = 299,
        [RecastGroup("Clarity", "Clarity", true)]
        Clarity = 300,
        [RecastGroup("Claw", "Claw", true)]
        Claw = 301,
        [RecastGroup("Cleanse Order", "Cleanse Order", true)]
        CleanseOrder = 302,
        [RecastGroup("Cluster Grenade", "Cluster Gren", true)]
        ClusterGrenade = 303,
        [RecastGroup("Coagulant", "Coagulant", true)]
        Coagulant = 304,
        [RecastGroup("Collapse Will", "Collapse Will", true)]
        CollapseWill = 305,
        [RecastGroup("Comprehend Speech", "Comp. Speech", true)]
        ComprehendSpeech = 306,
        [RecastGroup("Concussion Grenade", "Concuss Gren", true)]
        ConcussionGrenade = 307,
        [RecastGroup("Coordinated Strike", "Coord. Strike", true)]
        CoordinatedStrike = 308,
        [RecastGroup("Creeping Terror", "Creep Terror", true)]
        CreepingTerror = 309,
        [RecastGroup("Crushing Slam", "Crushing Slam", true)]
        CrushingSlam = 310,
        [RecastGroup("Cryo Sprayer", "Cryo Sprayer", true)]
        CryoSprayer = 311,
        [RecastGroup("Dampening Field", "Damp Field", true)]
        DampeningField = 312,
        [RecastGroup("Decisive Command", "Decisive Cmd", true)]
        DecisiveCommand = 313,
        [RecastGroup("Deflector Shield", "Deflect Shield", true)]
        DeflectorShield = 314,
        [RecastGroup("Devouring Strike", "Devour Strike", true)]
        DevouringStrike = 315,
        [RecastGroup("Distracting Feint", "Distract Feint", true)]
        DistractingFeint = 316,
        [RecastGroup("Dominate Weak Mind", "Dom Weak Mind", true)]
        DominateWeakMind = 317,
        [RecastGroup("Eclipse of Resolve", "Eclipse Res.", true)]
        EclipseOfResolve = 318,
        [RecastGroup("Emergency Bunker", "Emerg. Bunker", true)]
        EmergencyBunker = 319,
        [RecastGroup("Emergency Cocktail", "Emerg Cocktail", true)]
        EmergencyCocktail = 320,
        [RecastGroup("Emergency Sealant", "Emerg Sealant", true)]
        EmergencySealant = 321,
        [RecastGroup("Emergency Triage", "Emerg Triage", true)]
        EmergencyTriage = 322,
        [RecastGroup("Evasive Challenge", "Evas Challenge", true)]
        EvasiveChallenge = 323,
        [RecastGroup("Evasive Maneuver", "Evas Maneuver", true)]
        EvasiveManeuver = 324,
        [RecastGroup("Execute Prey", "Execute Prey", true)]
        ExecutePrey = 325,
        [RecastGroup("Expose Prey", "Expose Prey", true)]
        ExposePrey = 326,
        [RecastGroup("Flamethrower", "Flamethrower", true)]
        Flamethrower = 327,
        [RecastGroup("Flash Grenade", "Flash Grenade", true)]
        FlashGrenade = 328,
        [RecastGroup("Focus Stim", "Focus Stim", true)]
        FocusStim = 329,
        [RecastGroup("Force Body", "Force Body", true)]
        ForceBody = 330,
        [RecastGroup("Force-Bonded Beast", "Bonded Beast", true)]
        ForceBondedBeast = 331,
        [RecastGroup("Force Choke", "Force Choke", true)]
        ForceChoke = 332,
        [RecastGroup("Force Drain", "Force Drain", true)]
        ForceDrain = 333,
        [RecastGroup("Force Grip", "Force Grip", true)]
        ForceGrip = 334,
        [RecastGroup("Force Intercept", "Force Interc.", true)]
        ForceIntercept = 335,
        [RecastGroup("Force Leap", "Force Leap", true)]
        ForceLeap = 336,
        [RecastGroup("Force Lightning", "Force Lightng", true)]
        ForceLightning = 337,
        [RecastGroup("Force Maelstrom", "Force Maelstr", true)]
        ForceMaelstrom = 338,
        [RecastGroup("Force Mend", "Force Mend", true)]
        ForceMend = 339,
        [RecastGroup("Force Push", "Force Push", true)]
        ForcePush = 340,
        [RecastGroup("Force Rage", "Force Rage", true)]
        ForceRage = 341,
        [RecastGroup("Force Sanctuary", "Force Sanct.", true)]
        ForceSanctuary = 342,
        [RecastGroup("Force Spark", "Force Spark", true)]
        ForceSpark = 343,
        [RecastGroup("Force Touch", "Force Touch", true)]
        ForceTouch = 344,
        [RecastGroup("Fracture Focus", "Fracture Focus", true)]
        FractureFocus = 345,
        [RecastGroup("Frag Grenade", "Frag Grenade", true)]
        FragGrenade = 346,
        [RecastGroup("Group Deflector", "Group Deflect", true)]
        GroupDeflector = 347,
        [RecastGroup("Guarded Bite", "Guarded Bite", true)]
        GuardedBite = 348,
        [RecastGroup("Guardian Ward", "Guardian Ward", true)]
        GuardianWard = 349,
        [RecastGroup("Guarding Roar", "Guarding Roar", true)]
        GuardingRoar = 350,
        [RecastGroup("Rayshield Screen", "Rayshield Scrn", true)]
        RayshieldScreen = 351,
        [RecastGroup("Hasten", "Hasten", true)]
        Hasten = 352,
        [RecastGroup("Hold the Line", "Hold the Line", true)]
        HoldTheLine = 353,
        [RecastGroup("Hunger of the Dark", "Dark Hunger", true)]
        HungerOfTheDark = 354,
        [RecastGroup("Ice Breath", "Ice Breath", true)]
        IceBreath = 355,
        [RecastGroup("Incendiary Field", "Incend Field", true)]
        IncendiaryField = 356,
        [RecastGroup("Infusion", "Infusion", true)]
        Infusion = 357,
        [RecastGroup("Innervate", "Innervate", true)]
        Innervate = 358,
        [RecastGroup("Intercept", "Intercept", true)]
        Intercept = 359,
        [RecastGroup("Ion Grenade", "Ion Grenade", true)]
        IonGrenade = 360,
        [RecastGroup("Iron Hide", "Iron Hide", true)]
        IronHide = 361,
        [RecastGroup("Killzone Beacon", "Killzone Bcn", true)]
        KillzoneBeacon = 362,
        [RecastGroup("Kolto Mist", "Kolto Mist", true)]
        KoltoMist = 363,
        [RecastGroup("Last Stand of the Light", "Light's Stand", true)]
        LastStandOfTheLight = 364,
        [RecastGroup("Pulse Relay", "Pulse Relay", true)]
        PulseRelay = 365,
        [RecastGroup("Mark Target", "Mark Target", true)]
        MarkTarget = 366,
        [RecastGroup("Med Kit", "Med Kit", true)]
        MedKit = 367,
        [RecastGroup("Mind Shroud", "Mind Shroud", true)]
        MindShroud = 368,
        [RecastGroup("Mind Trick", "Mind Trick", true)]
        MindTrick = 369,
        [RecastGroup("Nightmare Field", "Nightmare Fld", true)]
        NightmareField = 370,
        [RecastGroup("Overload Barrage", "Overload Barr", true)]
        OverloadBarrage = 371,
        [RecastGroup("Pacify", "Pacify", true)]
        Pacify = 372,
        [RecastGroup("Pack Recovery", "Pack Recovery", true)]
        PackRecovery = 373,
        [RecastGroup("Pain Suppressant", "Pain Suppress", true)]
        PainSuppressant = 374,
        [RecastGroup("Poison Breath", "Poison Breath", true)]
        PoisonBreath = 375,
        [RecastGroup("Pounce", "Pounce", true)]
        Pounce = 376,
        [RecastGroup("Power Cell", "Power Cell", true)]
        PowerCell = 377,
        [RecastGroup("Predator Rush", "Predator Rush", true)]
        PredatorRush = 378,
        [RecastGroup("Predator's Mark", "Predator Mark", true)]
        PredatorsMark = 379,
        [RecastGroup("Press the Attack", "Press Attack", true)]
        PressTheAttack = 380,
        [RecastGroup("Primal Overrun", "Primal Overrun", true)]
        PrimalOverrun = 381,
        [RecastGroup("Psychic Cry", "Psychic Cry", true)]
        PsychicCry = 382,
        [RecastGroup("Purifying Wave", "Purifying Wave", true)]
        PurifyingWave = 383,
        [RecastGroup("Rail Dart", "Rail Dart", true)]
        RailDart = 384,
        [RecastGroup("Rampage", "Rampage", true)]
        Rampage = 385,
        [RecastGroup("Rampart Hide", "Rampart Hide", true)]
        RampartHide = 386,
        [RecastGroup("Reflective Barrier", "Reflect Barr", true)]
        ReflectiveBarrier = 387,
        [RecastGroup("Remote Charge", "Remote Charge", true)]
        RemoteCharge = 388,
        [RecastGroup("Rending Claw", "Rending Claw", true)]
        RendingClaw = 389,
        [RecastGroup("Renewal", "Renewal", true)]
        Renewal = 390,
        [RecastGroup("Resuscitation", "Resuscitation", true)]
        Resuscitation = 391,
        [RecastGroup("Rousing Shout", "Rousing Shout", true)]
        RousingShout = 392,
        [RecastGroup("Saber Rend", "Saber Rend", true)]
        SaberRend = 393,
        [RecastGroup("Shielding", "Shielding", true)]
        Shielding = 394,
        [RecastGroup("Shock Beacon", "Shock Beacon", true)]
        ShockBeacon = 395,
        [RecastGroup("Sonic Burst", "Sonic Burst", true)]
        SonicBurst = 396,
        [RecastGroup("Soothing Guard", "Soothing Guard", true)]
        SoothingGuard = 397,
        [RecastGroup("Thermal Detonator", "Thermal Det.", true)]
        ThermalDetonator = 398,
        [RecastGroup("Treatment Kit", "Treatment Kit", true)]
        TreatmentKit = 399,
        [RecastGroup("Unbreakable Beast", "Unbreak Beast", true)]
        UnbreakableBeast = 400,
        [RecastGroup("Untouchable Instinct", "Untouch Inst.", true)]
        UntouchableInstinct = 401,
        [RecastGroup("Warding Howl", "Warding Howl", true)]
        WardingHowl = 402,
        [RecastGroup("Weaken Resolve", "Weaken Resolve", true)]
        WeakenResolve = 403,
        [RecastGroup("Weapon Jam", "Weapon Jam", true)]
        WeaponJam = 404,
        [RecastGroup("Wrist Rocket", "Wrist Rocket", true)]
        WristRocket = 405,
        [RecastGroup("Rallying Standard", "Rally Std", true)]
        RallyingStandard = 406,
        [RecastGroup("Coordinated Focus", "Coord Focus", true)]
        CoordinatedFocus = 407,
        [RecastGroup("Charge Order", "Charge Order", true)]
        ChargeOrder = 408,
        [RecastGroup("Watchful Presence", "Watchful Pres", true)]
        WatchfulPresence = 409,
        [RecastGroup("Steady Formation", "Steady Form", true)]
        SteadyFormation = 410,
        [RecastGroup("Field Recovery", "Field Recov.", true)]
        FieldRecovery = 411,
        [RecastGroup("Capstone Ability", "Capstone", true)]
        Capstone = 412,
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
