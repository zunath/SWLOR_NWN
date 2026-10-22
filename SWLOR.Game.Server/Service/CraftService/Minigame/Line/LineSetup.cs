using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// Everything the Line engine needs to know about one fight besides chance: the angler's line, the fish's strength and how it
/// moves. It is immutable. <see cref="ForFish"/> derives it from the fish's level and rarity, the angler's Agriculture rank, the
/// rod's tier and the angler's perks; the constructor takes the finished numbers so a test can set them directly.
/// </summary>
public sealed class LineSetup
{
    /// <param name="tensionCap">The Tension at which the line snaps, before perks.</param>
    /// <param name="reelDistance">The Distance one Reel closes, before perks.</param>
    /// <param name="startDistance">The Distance the fish is hooked at, from 1 to the maximum Distance less 1.</param>
    /// <param name="distanceMax">The Distance at which the fish escapes.</param>
    /// <param name="stamina">The fish's Stamina.</param>
    /// <param name="pull">The fish's pull, the strength of its moves.</param>
    /// <param name="pattern">How the fish picks its moves.</param>
    /// <param name="effects">The angler's perks, or null for none.</param>
    /// <param name="fishLevel">The fish's level, for display and reports.</param>
    /// <param name="rarity">The fish's rarity, for display and reports.</param>
    /// <param name="rank">The angler's Agriculture rank, for display and reports.</param>
    /// <param name="rodTier">The rod's tier, 0 to 4, for display and reports.</param>
    public LineSetup(
        int tensionCap,
        int reelDistance,
        int startDistance,
        int distanceMax,
        int stamina,
        int pull,
        LineMovePattern pattern,
        LineAnglerEffects effects = null,
        int fishLevel = 0,
        FishRarity rarity = FishRarity.Common,
        int rank = 0,
        int rodTier = 0)
    {
        effects ??= LineAnglerEffects.None;
        if (tensionCap < 1)
            throw new ArgumentOutOfRangeException(nameof(tensionCap), "The line must hold some Tension.");
        if (reelDistance < 1)
            throw new ArgumentOutOfRangeException(nameof(reelDistance), "A Reel closes at least 1 Distance.");
        if (startDistance < 1 || startDistance >= distanceMax)
            throw new ArgumentOutOfRangeException(nameof(startDistance), "The fish starts between landed and escaped.");
        if (stamina < 1)
            throw new ArgumentOutOfRangeException(nameof(stamina), "The fish has some Stamina.");
        if (pull < 0)
            throw new ArgumentOutOfRangeException(nameof(pull), "The fish's pull cannot be negative.");
        if (effects.ForecastDepth < 1 || effects.ForecastDepth > 2)
            throw new ArgumentOutOfRangeException(nameof(effects), "The window shows the fish's next move or next two.");

        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        Effects = effects;
        TensionCap = tensionCap + effects.TensionCapBonus;
        ReelDistance = reelDistance + effects.ReelDistanceBonus;
        ReelTension = LineRules.ReelTension + effects.ReelTensionBonus;
        GiveLineDistance = Math.Max(0, LineRules.GiveLineDistance - effects.GiveLineDistanceReduction);
        SetHookDrain = LineRules.SetHookDrain + effects.SetHookDrainBonus;
        StartDistance = startDistance;
        DistanceMax = distanceMax;
        Stamina = stamina;
        Pull = pull;
        FishLevel = fishLevel;
        Rarity = rarity;
        Rank = rank;
        RodTier = rodTier;
    }

    /// <summary>The Tension at which the line snaps.</summary>
    public int TensionCap { get; }

    /// <summary>The Distance one Reel closes.</summary>
    public int ReelDistance { get; }

    /// <summary>The Tension one Reel adds.</summary>
    public int ReelTension { get; }

    /// <summary>The Distance Give Line lets out.</summary>
    public int GiveLineDistance { get; }

    /// <summary>The Stamina Set Hook drains.</summary>
    public int SetHookDrain { get; }

    public int StartDistance { get; }

    /// <summary>The Distance at which the fish escapes.</summary>
    public int DistanceMax { get; }

    /// <summary>The fish's Stamina when it is hooked.</summary>
    public int Stamina { get; }

    /// <summary>The strength of the fish's moves.</summary>
    public int Pull { get; }

    public LineMovePattern Pattern { get; }

    public LineAnglerEffects Effects { get; }

    /// <summary>How many of the fish's next moves the window shows.</summary>
    public int ForecastDepth => Effects.ForecastDepth;

    public int FishLevel { get; }

    public FishRarity Rarity { get; }

    public int Rank { get; }

    public int RodTier { get; }

    /// <summary>How much harder the fish is than the angler's rank: its level less the rank.</summary>
    public int Difficulty => FishLevel - Rank;

    /// <summary>The Tension cap before perks, from the rod tier and the Agriculture rank.</summary>
    public static int BaseTensionCap(int rodTier, int rank) =>
        LineRules.TensionCapBase + rodTier / LineRules.RodTiersPerTensionCapPoint + rank / LineRules.RanksPerTensionCapPoint;

    /// <summary>The Distance a Reel closes before perks, from the rod tier and the Agriculture rank.</summary>
    public static int BaseReelDistance(int rodTier, int rank) =>
        LineRules.ReelDistanceBase + (rodTier + rank / LineRules.RanksPerPower) / LineRules.PowerPerReelDistancePoint;

    /// <summary>The Distance the fish is hooked at: farther for an angler who reels farther, so a fight takes about as many reels.</summary>
    public static int BaseStartDistance(int reelDistance) =>
        LineRules.StartDistanceBase + reelDistance * LineRules.StartDistancePerReelDistance;

    /// <summary>The Distance at which the fish escapes, a couple of reels beyond where it is hooked.</summary>
    public static int BaseDistanceMax(int reelDistance) =>
        BaseStartDistance(reelDistance) + LineRules.EscapeBufferBase + reelDistance * LineRules.EscapeBufferPerReelDistance;

    /// <summary>
    /// The rung of the difficulty ladder for a fish: <see cref="LineRules.RungAtRank"/> when its level equals the rank, and
    /// <see cref="LineRules.RungsPerLevel"/> more for each level above it, kept between the first and last rung.
    /// </summary>
    public static int Rung(int fishLevel, int rank) =>
        Math.Clamp(
            LineRules.RungAtRank + LineRules.RungsPerLevel * (fishLevel - rank),
            LineRules.RungMinimum,
            LineRules.RungMaximum);

    /// <summary>The fish's pull on a rung, as a share of the Tension cap before perks.</summary>
    public static int PullAt(int rung, int baseTensionCap) =>
        Math.Max(1, Rounded(baseTensionCap * (LineRules.PullPercentOfCapAtRung0 + LineRules.PullPercentPerRung * rung) / 100.0));

    /// <summary>The fish's Stamina on a rung.</summary>
    public static int StaminaAt(int rung) =>
        Math.Max(1, Rounded(LineRules.StaminaAtRung0 + LineRules.StaminaPerRung * rung));

    /// <summary>The weight of a rest among the fish's moves on a rung: the higher the rung, the less it rests.</summary>
    public static int TireWeightAt(int rung) =>
        Math.Max(LineRules.TireWeightMinimum, Rounded(LineRules.TireWeightAtRung0 - LineRules.TireWeightPerRung * rung));

    /// <summary>
    /// The fight for a fish of the given level and rarity, hooked by an angler of the given Agriculture rank with a rod of the
    /// given tier. The rod and the rank set the line (Tension cap and reel strength). The fish's level against the rank picks its
    /// rung, which sets its pull, Stamina and how often it rests, and a rare fish also chains its moves.
    /// </summary>
    public static LineSetup ForFish(
        int fishLevel,
        FishRarity rarity,
        int rank,
        int rodTier,
        LineAnglerEffects effects = null)
    {
        effects ??= LineAnglerEffects.None;
        var rung = Rung(fishLevel, rank);
        if (rarity == FishRarity.Rare)
            rung = Math.Clamp(rung - effects.RareFishRungReduction, LineRules.RungMinimum, LineRules.RungMaximum);

        var reelDistance = BaseReelDistance(rodTier, rank);
        var tensionCap = BaseTensionCap(rodTier, rank);
        return new LineSetup(
            tensionCap,
            reelDistance,
            BaseStartDistance(reelDistance),
            BaseDistanceMax(reelDistance),
            StaminaAt(rung),
            PullAt(rung, tensionCap),
            LineMovePattern.For(rarity, TireWeightAt(rung)),
            effects,
            fishLevel,
            rarity,
            rank,
            rodTier);
    }

    private static int Rounded(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
