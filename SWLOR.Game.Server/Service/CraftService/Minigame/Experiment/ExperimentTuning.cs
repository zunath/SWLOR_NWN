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
public sealed record ExperimentTuning(
    int Trials = ExperimentRules.Trials,
    int TestChanceBonusPercent = 0,
    int RefineChanceBonus = ExperimentRules.RefineChanceBonusPercent,
    int RefineCp = ExperimentRules.RefineCp,
    int TimeCutPerEvidence = ExperimentRules.TimeCutPercentPerEvidence,
    int TimeCutCap = ExperimentRules.TimeCutCapPercent)
{
    public static readonly ExperimentTuning Default = new();
}
