namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>
/// Every starting value of the research Experiment, in one place for the simulator to tune. The engine reads these and nothing else.
/// How evidence becomes a bonus (<c>ResearchOutcomeRules</c>) and what the experiment earns (<c>ResearchXpRules</c>) are tuned there.
/// </summary>
public static class ExperimentRules
{
    /// <summary>The trials every experiment has. A Test or an Analyze uses one; Refine Method and Conclude use none.</summary>
    public const int Trials = 6;

    /// <summary>The chance a Test finds evidence for a researcher at the lowest Fabrication rank that can research the recipe, in percent.</summary>
    public const int BaseTestChancePercent = 60;

    /// <summary>Each Fabrication rank above the recipe's research tier adds one point of Test chance, up to this many.</summary>
    public const int RankChanceBonusMaximum = 15;

    public const int MinimumTestChancePercent = 5;
    public const int MaximumTestChancePercent = 95;

    /// <summary>What Refine Method adds to the next Test's chance, in points.</summary>
    public const int RefineChanceBonusPercent = 15;

    /// <summary>The Fabrication CP Refine Method costs. It is the only thing CP pays for, so it sets how often a researcher can refine.</summary>
    public const int RefineCp = 30;

    /// <summary>Each piece of spare evidence Analyze spends cuts this share of the job's time, in percent.</summary>
    public const int TimeCutPercentPerEvidence = 5;

    /// <summary>The most the experiment can cut from the job's time, in percent of the job.</summary>
    public const int TimeCutCapPercent = 25;

    /// <summary>The fewest CP Refine Method can cost after a perk discount.</summary>
    public const int MinimumRefineCp = 1;

    /// <summary>The fewest trials an experiment can have after perks.</summary>
    public const int MinimumTrials = 1;

    /// <summary>The Fabrication CP Peer Review costs. It uses no trial and can be used once per experiment.</summary>
    public const int PeerReviewCp = 25;

    /// <summary>After Breakthrough, each piece of spare evidence Analyze spends cuts this many times its usual share of the job's time.</summary>
    public const int BreakthroughTimeCutMultiplier = 2;

    // CP: gear CP plus this share of the rank. The rank-25 passive the other minigames add is re-tuned to nothing here, because
    // Refine Method is the only CP sink and a flat bonus would make it free for every researcher from rank 25.
    public const double CpPerRank = 0.75;
    public const int RankPassiveCpRank = 25;
    public const int RankPassiveCp = 0;
}
