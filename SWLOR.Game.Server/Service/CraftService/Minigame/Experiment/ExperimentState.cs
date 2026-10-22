using System.Collections.Immutable;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>Everything the Experiment knows about an attempt: the shared meters, the evidence on each candidate and the job's time cut.</summary>
/// <param name="Meters">
/// Trials used (progress), Findings (quality: the leading candidate's evidence plus the evidence Analyze spent), an Integrity meter that
/// never changes because an experiment cannot fail, and CP.
/// </param>
/// <param name="NextOpportunity">Always null: the extra bonus is decided before the first action and shown for the whole experiment.</param>
/// <param name="Evidence">The evidence on each candidate, in the order of <see cref="ExperimentSetup.Candidates"/>.</param>
/// <param name="SpentEvidence">The evidence Analyze has turned into time cut.</param>
/// <param name="TimeCutPercent">The share of the job's time cut so far, in percent.</param>
/// <param name="MethodBonus">The Test chance Refine Method has added to the next Test, in points; 0 when none is waiting.</param>
/// <param name="Concluded">True once the player ended the experiment.</param>
public sealed record ExperimentState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    ImmutableArray<int> Evidence,
    int SpentEvidence,
    int TimeCutPercent,
    int MethodBonus,
    bool Concluded) : IMinigameState<ExperimentState>
{
    public ExperimentState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>The most evidence on any one candidate.</summary>
    public int LeadingEvidence => Evidence.IsDefaultOrEmpty ? 0 : Evidence.Max();

    public int TotalEvidence => Evidence.IsDefaultOrEmpty ? 0 : Evidence.Sum();
}
