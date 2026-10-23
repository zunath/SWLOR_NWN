namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>
/// Everything the Search engine needs to know about one site besides chance: how many searches the scavenger gets, how many spots
/// are promising, and the chances. It is immutable. <see cref="ForRank"/> derives it from the Gathering rank and the scavenger's
/// perks; the constructor takes the finished numbers so a test can set them directly.
/// </summary>
public sealed class SearchSetup
{
    /// <param name="searches">The searches the scavenger gets; every action costs one.</param>
    /// <param name="promisingSpots">The spots of the 3x3 board that are promising, from 0 to 9, perks included.</param>
    /// <param name="ordinaryChance">The chance, in percent, that a Search of ordinary ground finds something, perks included.</param>
    /// <param name="promisingChance">The chance, in percent, that a Search of promising ground finds something, perks included.</param>
    /// <param name="digRiskBase">The risk, in percent, that the first Dig Deeper at a spot ruins it, before perks.</param>
    /// <param name="digRiskStep">The risk added for each Dig Deeper already made at the spot, before perks.</param>
    /// <param name="digFindChance">The chance, in percent, that a Dig Deeper that does not ruin the spot finds a rare item, perks included.</param>
    /// <param name="effects">The scavenger's perks, or null for none. Their effect on the numbers above is applied by <see cref="ForRank"/>
    /// and the constructor only keeps the risk reduction, which a dig applies.</param>
    /// <param name="rank">The scavenger's Gathering rank, for display and reports.</param>
    /// <param name="siteLevel">The site's level, for display and reports.</param>
    public SearchSetup(
        int searches,
        int promisingSpots,
        int ordinaryChance,
        int promisingChance,
        int digRiskBase,
        int digRiskStep,
        int digFindChance,
        SearchEffects effects = null,
        int rank = 0,
        int siteLevel = 0)
    {
        effects ??= SearchEffects.None;
        if (searches < 1)
            throw new ArgumentOutOfRangeException(nameof(searches), "The scavenger has at least one search.");
        if (promisingSpots < 0 || promisingSpots > SearchBoard.Spots)
            throw new ArgumentOutOfRangeException(nameof(promisingSpots), "The board has 0 to 9 promising spots.");
        if (ordinaryChance < 0 || ordinaryChance > 100 || promisingChance < 0 || promisingChance > 100)
            throw new ArgumentOutOfRangeException(nameof(ordinaryChance), "A chance is 0 to 100 percent.");
        if (digRiskBase < 0 || digRiskStep < 0)
            throw new ArgumentOutOfRangeException(nameof(digRiskBase), "A risk cannot be negative.");
        if (digFindChance < 0 || digFindChance > 100)
            throw new ArgumentOutOfRangeException(nameof(digFindChance), "A chance is 0 to 100 percent.");
        if (effects.DigRiskPercent < 1 || effects.DigRiskPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(effects), "The share of the dig risk that is left is 1 to 100 percent.");

        Searches = searches;
        PromisingSpots = promisingSpots;
        OrdinaryChance = ordinaryChance;
        PromisingChance = promisingChance;
        DigRiskBase = digRiskBase;
        DigRiskStep = digRiskStep;
        DigFindChance = digFindChance;
        Effects = effects;
        Rank = rank;
        SiteLevel = siteLevel;
    }

    /// <summary>The searches the scavenger gets.</summary>
    public int Searches { get; }

    /// <summary>The promising spots on the board.</summary>
    public int PromisingSpots { get; }

    public int OrdinaryChance { get; }

    public int PromisingChance { get; }

    public int DigRiskBase { get; }

    public int DigRiskStep { get; }

    public int DigFindChance { get; }

    public SearchEffects Effects { get; }

    public int Rank { get; }

    public int SiteLevel { get; }

    /// <summary>The chance, in percent, that a Search finds something on ground of the given kind, for ground the scavenger has not learned about use <see cref="UnknownChanceText"/>.</summary>
    public int SearchChance(bool promising) => promising ? PromisingChance : OrdinaryChance;

    /// <summary>
    /// The risk, in percent, that a Dig Deeper ruins a spot that has already been dug the given number of times: the base risk and a step
    /// for each dig before it, kept to <see cref="SearchRules.DigRiskMaximum"/>, then the share of it that the scavenger's perks leave.
    /// </summary>
    public int DigRisk(int digsAlreadyMade)
    {
        var risk = Math.Min(SearchRules.DigRiskMaximum, DigRiskBase + DigRiskStep * digsAlreadyMade);
        return (risk * Effects.DigRiskPercent + 50) / 100;
    }

    /// <summary>The words that tell a scavenger what a Search of ground they know nothing about can do: both chances, never which applies.</summary>
    public string UnknownChanceText => $"{OrdinaryChance}% on ordinary ground, {PromisingChance}% on promising ground";

    /// <summary>
    /// The setup for a scavenger of the Gathering rank at a site of the level. The rank sets the searches
    /// (<see cref="CraftRankMilestones.SearchesForRank"/>) and the promising spots (<see cref="CraftRankMilestones.PromisingSpotsForRank"/>);
    /// the perks add promising spots, change the chance of a Search, and reduce the dig risk or raise its find chance. No attribute is read.
    /// </summary>
    public static SearchSetup ForRank(int gatheringRank, int siteLevel, SearchEffects effects = null)
    {
        effects ??= SearchEffects.None;
        var promising = Math.Min(SearchBoard.Spots, CraftRankMilestones.PromisingSpotsForRank(gatheringRank) + Math.Max(0, effects.ExtraPromisingSpots));
        return new SearchSetup(
            CraftRankMilestones.SearchesForRank(gatheringRank),
            promising,
            ClampChance(SearchRules.OrdinaryChance + effects.SearchChanceDelta),
            ClampChance(SearchRules.PromisingChance + effects.SearchChanceDelta),
            SearchRules.DigRiskBase,
            SearchRules.DigRiskStep,
            Math.Clamp(SearchRules.DigFindChance + effects.DigFindBonus, SearchRules.MinimumChance, SearchRules.MaximumDigFindChance),
            effects,
            gatheringRank,
            siteLevel);
    }

    private static int ClampChance(int chance) => Math.Clamp(chance, SearchRules.MinimumChance, SearchRules.MaximumChance);
}
