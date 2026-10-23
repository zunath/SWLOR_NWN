namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>
/// The numbers of the Search board. Every value here is a starting value for the simulator to tune; the engine and the setup read
/// them and nothing else, so a tuning pass changes this file (and the rank schedule in <see cref="CraftRankMilestones"/>) only.
/// How many searches a scavenger gets, how many spots are promising and how often a Search of promising ground finds come from
/// Gathering rank; they replaced Hard Look and Perception (<see cref="SearchSetup.ForRank"/>).
/// </summary>
public static class SearchRules
{
    // The ground. A Search finds with this chance, by the ground at the spot. Promising ground is the point of Sifting: it finds
    // several times as often as ordinary ground (3.6 times at Gathering 0, 5.4 times at Gathering 40), at the chance the rank gives
    // (CraftRankMilestones.PromisingChanceForRank, 29% to 43%).
    public const int OrdinaryChance = 8;
    public const int MinimumChance = 1;
    public const int MaximumChance = 95;

    // Dig Deeper. It searches a spot that was already searched, once more, with much better odds of a rare item. Each dig at the same spot risks ruining it:
    // the risk is DigRiskBase for the first dig and DigRiskStep more for each dig before it, kept to DigRiskMaximum. A dig that does
    // not ruin the spot finds with DigFindChance percent; the find rolls the loot table with its rare entries favoured (ScavengeLoot).
    public const int DigRiskBase = 30;
    public const int DigRiskStep = 20;
    public const int DigRiskMaximum = 90;
    public const int DigFindChance = 20;
    public const int MaximumDigFindChance = 95;

    // The pace of the board. An action takes the player this many seconds, reading the previews included; the CraftSim study and the
    // XP multipliers below assume it. The lifecycle enforces it as the least time between two actions (the first is free), so fast
    // clicking cannot finish a board in seconds and be paid the multiplier for it.
    public const double ActionSeconds = 6.0;

    // Quick Search. It takes as long as today's search: the old code played one two-second get-low animation for the whole visit, before
    // its one or two attempts, so a second attempt added no time.
    public const double QuickSearchSeconds = 2.0;

    // The multiplier on the XP of a Search or Dig Deeper attempt (see CraftMinigameXp), by the site's level: the least that keeps the
    // CraftSim XP-rate gate (gate 6) passing at the site level. Today a visit takes two seconds of animation however many attempts it
    // makes; the board takes six seconds an action, but it has more attempts for the time, so today's XP per attempt nearly passes.
    // Starting values until client timing replaces the seconds per action; 1.0 at every level restores today's XP per attempt. Level 0
    // sites (no perk, no tier) use the level 1 value.
    public const double XpMultiplierLevel1 = 1.05;
    public const double XpMultiplierLevel2 = 1.05;
    public const double XpMultiplierLevel3 = 1.10;
    public const double XpMultiplierLevel4 = 1.12;
    public const double XpMultiplierLevel5 = 1.12;

    /// <summary>
    /// The Gathering XP of one action: what the site pays against the scavenger's rank (a success pays all of
    /// <paramref name="deltaXp"/>, a failure a quarter of it, as scavenging always did), times the board's multiplier for the site's level.
    /// A Sift finds nothing, so it pays the quarter an attempt that finds nothing pays.
    /// </summary>
    public static int AttemptXp(int deltaXp, int siteLevel, bool success) =>
        CraftMinigameXp.ScaleBaseXp(success ? deltaXp : deltaXp / 4, XpMultiplierForLevel(siteLevel));

    /// <summary>The multiplier on the XP of an attempt at a site of the level. A level of 0 or less takes level 1's.</summary>
    public static double XpMultiplierForLevel(int siteLevel) => siteLevel switch
    {
        <= 1 => XpMultiplierLevel1,
        2 => XpMultiplierLevel2,
        3 => XpMultiplierLevel3,
        4 => XpMultiplierLevel4,
        _ => XpMultiplierLevel5
    };
}
