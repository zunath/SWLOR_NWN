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
    public const int StartDistanceBase = 4;
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
    // its own tier and perks that raise the cap do not make the fish weaker.
    public const int RungAtRank = 42;
    public const int RungsPerLevel = 5;
    public const int RungMinimum = 0;
    public const int RungMaximum = 100;
    public const double PullPercentOfCapAtRung0 = 10;
    public const double PullPercentPerRung = 0.8;
    public const double StaminaAtRung0 = 8;
    public const double StaminaPerRung = 0.24;
    public const double TireWeightAtRung0 = 45;
    public const double TireWeightPerRung = 0.45;
    public const int TireWeightMinimum = 2;
}
