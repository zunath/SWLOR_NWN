namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// The numbers of the Line minigame. Every value here is a starting value for the simulator to tune; the engine and the setup
/// read them and nothing else, so a tuning pass changes this file only. The angler's line comes from the rod tier and the
/// Agriculture rank, and the fish's strength from its level against the rank (<see cref="LineSetup.ForFish"/>).
/// </summary>
public static class LineRules
{
    public const int StartTension = 0;

    // The angler. Power is the rod tier (0-4) plus one for every 10 ranks of Agriculture. The rod tier and the rank set the
    // Tension cap and the Distance a Reel closes, which replace the Willpower bonus fishing had.
    public const int RanksPerPower = 10;
    public const int TensionCapBase = 20;
    public const int TensionCapPerRodTier = 1;
    public const int RanksPerTensionCapPoint = 12;
    public const int ReelDistanceBase = 6;
    public const int PowerPerReelDistanceStep = 6;
    public const int ReelDistancePerStep = 2;

    // The line. The fish is hooked about two reels out and escapes a couple of reels beyond that, whatever the reel strength.
    public const int StartDistanceBase = 5;
    public const int StartDistancePerReelDistance = 2;
    public const int EscapeBufferBase = 2;
    public const int EscapeBufferPerReelDistance = 2;

    // Actions, before the fish's move. Reel closes Distance and adds Tension; Give Line lowers Tension and lets out Distance;
    // Hold drains Stamina; Set Hook drains more, once, and the fish's first move is spent on it.
    public const int ReelTension = 4;
    public const int GiveLineDistance = 4;
    public const int GiveLineRelief = 6;
    public const int HoldTension = 2;
    public const int HoldDrain = 4;
    public const int SetHookTension = 2;
    public const int SetHookDrain = 6;

    // The fish's moves, in terms of its pull. A Run takes pull in Distance, or in Tension (pull plus RunReelStrain) if the
    // angler reels against it; a Dive adds the pull in Tension to anything but Give Line, which absorbs half; a Thrash adds
    // pull plus ThrashSpike in Tension, which only Hold absorbs entirely; a Tire is a free pull-in.
    public const int RunReelStrain = 2;
    public const int ThrashSpike = 2;
    public const int TireReelDistance = 2;
    public const int TireStaminaLoss = 2;

    // The fish's strength. Its level less the angler's rank picks a rung of a ladder from 0 (a minnow) to 100 (hopeless), and
    // every rung adds a little pull, Stamina and aggression, so the chance of landing it falls smoothly with each level the fish
    // is above the rank. The pull is a share of the angler's Tension cap before perks, so a better rod meets stronger fish of
    // its own tier and perks that raise the cap do not make the fish weaker. RungAtRank is where a fish of the angler's own level
    // sits. It is set so that a skilled angler with no perks lands slightly more fish than Quick Reel at the fish's own level (about
    // 1.03 times; the owner's reading of "fighting is optional, for a slightly better catch"), which leaves the Angler line the room
    // between that and the bound on the fight's catch rate (FishingParityOptions.FightBoundRelative in CraftSim).
    public const int RungAtRank = 44;
    public const int RungsPerLevel = 5;

    // A fish above the rank climbs the ladder faster than one below it descends: the first HardLevelsFrom levels above the rank take
    // RungsPerLevelAbove rungs each. Quick Reel loses 10 points a level, and a fight against fish above the rank must lose points at
    // least as fast so that it stays within the bound there too.
    public const int RungsPerLevelAbove = 7;

    // A fish more than HardLevelsFrom levels above the rank climbs the ladder faster, RungsPerHardLevel rungs for each level beyond
    // it: Quick Reel cannot land a fish 6 levels above the rank at all, and the fight must not either, whatever the angler's perks.
    public const int HardLevelsFrom = 3;
    public const int RungsPerHardLevel = 9;
    public const int RungMinimum = 0;
    public const int RungMaximum = 100;
    public const double PullPercentOfCapAtRung0 = 10;
    public const double PullPercentPerRung = 0.8;
    public const double StaminaAtRung0 = 8;
    public const double StaminaPerRung = 0.24;
    public const double TireWeightAtRung0 = 45;
    public const double TireWeightPerRung = 0.45;
    public const int TireWeightMinimum = 2;

    // The pace of the fight. A turn takes the player this many seconds, reading the previews included; the CraftSim study and the XP
    // multipliers below assume it. The fishing lifecycle enforces it as the least time between two actions (the first is free), so
    // fast clicking cannot finish a fight in seconds and be paid the multiplier for it.
    public const double TurnSeconds = 8.0;

    // Fish Sense. The angler who has it sees the fish's next two moves, but only on the first FishSenseTurns turns of a fight: after
    // that the window shows one move, as for everyone. The full forecast was the largest single purchase of the Angler line. Even
    // limited to the first turn it adds more at the fish's own level than the room under the fight bound leaves beside a style, so
    // the Angler draft holds it open for the owner.
    public const int FishSenseTurns = 1;

    // The multiplier on the XP of a bite (see CraftMinigameXp), by the fish's level band: the least that keeps the CraftSim XP-rate
    // gate (gate 6) passing in the band. Today's cast takes about seven seconds; the fight adds three to six turns of TurnSeconds to
    // that, so the same XP per bite would cut the XP per hour to a seventh. Today's XP is still paid at the bite, caught or not. The
    // rest of the multiplied XP is paid when the fight ends on its own (landed, snapped or escaped), in proportion to how long the
    // fight lasted up to XpFullTurns turns (XpShare), so neither abandoning a fight to cast again nor losing it on purpose in one
    // move earns more per hour than today. Starting values until client timing replaces the seconds per turn; 1.0 in every band
    // restores today's XP per bite.
    public const int XpFullTurns = 5;
    public const double XpMultiplierLevels0To10 = 8.55;
    public const double XpMultiplierLevels11To20 = 8.5;
    public const double XpMultiplierLevels21To30 = 8.0;
    public const double XpMultiplierLevels31To40 = 8.2;
    public const double XpMultiplierLevels41Up = 8.0;

    /// <summary>
    /// The share, from 0 to 1, of a fight's multiplied XP that a fight lasting <paramref name="fightSeconds"/> has earned: all of it from
    /// <see cref="XpFullTurns"/> turns, and in proportion below that.
    /// </summary>
    public static double XpShare(double fightSeconds) =>
        fightSeconds <= 0 ? 0 : Math.Min(1.0, fightSeconds / (XpFullTurns * TurnSeconds));

    /// <summary>The multiplier on the XP of a bite from a fish of the level. A level of 0 or less takes the lowest band's.</summary>
    public static double XpMultiplierForLevel(int fishLevel) => fishLevel switch
    {
        <= 10 => XpMultiplierLevels0To10,
        <= 20 => XpMultiplierLevels11To20,
        <= 30 => XpMultiplierLevels21To30,
        <= 40 => XpMultiplierLevels31To40,
        _ => XpMultiplierLevels41Up
    };
}
