using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>
/// The numbers of an experiment that a researcher's perks and the simulator can change. The default is
/// <see cref="ExperimentRules"/>. A setup holds one and the engine reads it through the setup.
/// </summary>
/// <param name="Trials">The trials the experiment has.</param>
/// <param name="TestChanceBonusPercent">Points added to every Test's chance before the cap (negative lowers it).</param>
/// <param name="RefineChanceBonus">What Refine Method adds to the next Test's chance, in points.</param>
/// <param name="RefineCp">The CP Refine Method costs.</param>
/// <param name="TimeCutPerEvidence">The percent of the job's time each piece of spare evidence cuts.</param>
/// <param name="TimeCutCap">The most the experiment can cut from the job's time, in percent.</param>
/// <param name="PeerReview">True when the experiment offers Peer Review.</param>
/// <param name="Breakthrough">True when the experiment offers Breakthrough.</param>
public sealed record ExperimentTuning(
    int Trials = ExperimentRules.Trials,
    int TestChanceBonusPercent = 0,
    int RefineChanceBonus = ExperimentRules.RefineChanceBonusPercent,
    int RefineCp = ExperimentRules.RefineCp,
    int TimeCutPerEvidence = ExperimentRules.TimeCutPercentPerEvidence,
    int TimeCutCap = ExperimentRules.TimeCutCapPercent,
    bool PeerReview = false,
    bool Breakthrough = false)
{
    public static readonly ExperimentTuning Default = new();

    /// <summary>
    /// The numbers the researcher's Researcher perks give, read once from their stat totals when an experiment is set up. No perk
    /// raises the time cut's cap. The trials and the CP cost keep their minimums.
    /// </summary>
    /// <param name="total">The researcher's total for a stat, such as <c>Stat.GetStatAdjustment</c> for the researching player.</param>
    public static ExperimentTuning FromStatTotals(Func<StatType, int> total)
    {
        if (total == null)
            throw new ArgumentNullException(nameof(total));

        return new ExperimentTuning(
            Math.Max(ExperimentRules.MinimumTrials, ExperimentRules.Trials + total(StatType.ExperimentTrials)),
            total(StatType.ExperimentTestChancePercent),
            ExperimentRules.RefineChanceBonusPercent + total(StatType.ExperimentRefineChancePercent),
            Math.Max(ExperimentRules.MinimumRefineCp, ExperimentRules.RefineCp - total(StatType.ExperimentRefineCpReduction)),
            ExperimentRules.TimeCutPercentPerEvidence + total(StatType.ExperimentTimeCutPerEvidencePercent),
            ExperimentRules.TimeCutCapPercent,
            total(StatType.ExperimentPeerReviewUnlocked) > 0,
            total(StatType.ExperimentBreakthroughUnlocked) > 0);
    }
}
