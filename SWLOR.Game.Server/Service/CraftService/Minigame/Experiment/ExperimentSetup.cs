using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>
/// Everything the Experiment engine takes as fixed for one experiment: the research level it belongs to, the researcher's rank and
/// gear, the extra bonus rolled when it opened, and the numbers those give. It holds no state; the engine's state record does.
/// </summary>
public sealed class ExperimentSetup
{
    /// <param name="targetLevel">The research level the job brings the blueprint to, 1 to 10.</param>
    /// <param name="rank">The researcher's Fabrication rank.</param>
    /// <param name="requiredRank">The lowest Fabrication rank that can research the recipe's tier.</param>
    /// <param name="gearCp">The Fabrication CP from the researcher's gear.</param>
    /// <param name="extraBonus">True when the extra guaranteed bonus was rolled for this job.</param>
    /// <param name="tuning">The numbers perks and the simulator change; null uses the starting values.</param>
    /// <param name="licensedRunsGrant">
    /// At a random level, the licensed runs (1 to 3) rolled when the experiment opened, or 0 when the level grants a reduction that
    /// evidence decides. Must be 0 at every other level.
    /// </param>
    public ExperimentSetup(
        int targetLevel,
        int rank,
        int requiredRank,
        int gearCp,
        bool extraBonus,
        ExperimentTuning tuning = null,
        int licensedRunsGrant = 0)
    {
        tuning ??= ExperimentTuning.Default;
        if (tuning.Trials < 1)
            throw new ArgumentOutOfRangeException(nameof(tuning), tuning.Trials, "An experiment has at least one trial.");

        if (targetLevel < 1 || targetLevel > ResearchGrantRules.MaxLevel)
            throw new ArgumentOutOfRangeException(nameof(targetLevel), targetLevel, "Research levels run 1 to 10.");

        if (licensedRunsGrant < 0 || licensedRunsGrant > ResearchGrantRules.LicensedRunsMaximum ||
            (licensedRunsGrant > 0 && !ResearchGrantRules.IsRandomLevel(targetLevel)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(licensedRunsGrant), licensedRunsGrant, "Only a random level rolls licensed runs, from 1 to 3.");
        }

        TargetLevel = targetLevel;
        LicensedRunsGrant = licensedRunsGrant;
        Candidates = ResearchGrantRules.CandidatesFor(targetLevel, licensedRunsGrant);
        Rank = Math.Max(0, rank);
        RequiredRank = Math.Max(0, requiredRank);
        GearCp = Math.Max(0, gearCp);
        ExtraBonus = extraBonus;
        ExtraBonusTier = ResearchGrantRules.ExtraBonusTier(targetLevel);

        Trials = tuning.Trials;
        RankChanceBonus = Math.Clamp(Rank - RequiredRank, 0, ExperimentRules.RankChanceBonusMaximum);
        BaseTestChancePercent = Math.Clamp(
            ExperimentRules.BaseTestChancePercent + RankChanceBonus + tuning.TestChanceBonusPercent,
            ExperimentRules.MinimumTestChancePercent,
            ExperimentRules.MaximumTestChancePercent);
        MaxCp = (int)(GearCp + Rank * ExperimentRules.CpPerRank) +
                (Rank >= ExperimentRules.RankPassiveCpRank ? ExperimentRules.RankPassiveCp : 0);
        RefineCp = tuning.RefineCp;
        RefineChanceBonus = tuning.RefineChanceBonus;
        TimeCutPerEvidence = tuning.TimeCutPerEvidence;
        TimeCutCap = tuning.TimeCutCap;
    }

    public int TargetLevel { get; }

    /// <summary>True at research levels 3, 5 and 8, which grant either licensed runs or a reduction.</summary>
    public bool IsRandomLevel => ResearchGrantRules.IsRandomLevel(TargetLevel);

    /// <summary>The licensed runs a random level rolled when the experiment opened; 0 when it grants a reduction, and at other levels.</summary>
    public int LicensedRunsGrant { get; }

    /// <summary>True when evidence decides between two candidates, credit reduction and time reduction.</summary>
    public bool IsContested => Candidates.Count > 1;

    /// <summary>
    /// The candidates the player gathers evidence toward: the level's own grant (a fixed level's, or licensed runs when they were
    /// rolled), or credit reduction and time reduction.
    /// </summary>
    public IReadOnlyList<ResearchCandidate> Candidates { get; }

    public int Rank { get; }
    public int RequiredRank { get; }
    public int GearCp { get; }
    public bool ExtraBonus { get; }

    /// <summary>The tier of the extra guaranteed bonus, if the job has one.</summary>
    public int ExtraBonusTier { get; }

    public int Trials { get; }

    /// <summary>The points of Test chance the researcher's rank adds.</summary>
    public int RankChanceBonus { get; }

    /// <summary>The chance of a Test finding evidence before Refine Method, in percent.</summary>
    public int BaseTestChancePercent { get; }

    public int MaxCp { get; }
    public int RefineCp { get; }
    public int RefineChanceBonus { get; }
    public int TimeCutPerEvidence { get; }
    public int TimeCutCap { get; }

    public int IndexOf(ResearchCandidate candidate)
    {
        for (var index = 0; index < Candidates.Count; index++)
        {
            if (Candidates[index] == candidate)
                return index;
        }

        throw new ArgumentException($"This experiment has no {candidate} candidate.", nameof(candidate));
    }

    public string NameOf(ResearchCandidate candidate) =>
        ResearchGrantRules.CandidateName(candidate, TargetLevel, LicensedRunsGrant);

    /// <summary>Builds the setup for a recipe a researcher is taking to <paramref name="targetLevel"/>.</summary>
    public static ExperimentSetup ForRecipe(
        RecipeDetail recipe,
        int targetLevel,
        int fabricationRank,
        int gearCp,
        bool extraBonus,
        ExperimentTuning tuning = null,
        int licensedRunsGrant = 0)
    {
        if (recipe == null)
            throw new ArgumentNullException(nameof(recipe));

        var tier = Math.Min(CraftRankMilestones.MaxResearchTier, recipe.Level / CraftRankMilestones.RanksPerTier + 1);
        return new ExperimentSetup(
            targetLevel,
            fabricationRank,
            CraftRankMilestones.RequiredFabricationRankForResearchTier(tier),
            gearCp,
            extraBonus,
            tuning,
            licensedRunsGrant);
    }
}
