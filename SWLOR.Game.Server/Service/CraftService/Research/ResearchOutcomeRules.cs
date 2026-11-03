using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// How an experiment's evidence becomes an outcome. At a random level that grants a reduction, evidence decides between credit
/// reduction and time reduction and sizes the winner. A reduction never exceeds today's d10 (10 percent), and no evidence on either
/// candidate means no outcome, so the job falls back to today's roll between them when it completes. Licensed runs are not decided by
/// evidence: they are rolled when the experiment opens.
/// </summary>
public static class ResearchOutcomeRules
{
    /// <summary>The evidence at which a reduction pays today's dice maximum. One piece of evidence pays the dice minimum.</summary>
    public const int EvidenceForMaximum = 6;

    // How often today's roll picks credit reduction and time reduction once licensed runs are ruled out: 400 each. Ties use the same odds.
    private static readonly IReadOnlyDictionary<ResearchCandidate, int> TieBreakWeights = new Dictionary<ResearchCandidate, int>
    {
        [ResearchCandidate.CreditReduction] = 400,
        [ResearchCandidate.TimeReduction] = 400
    };

    /// <summary>
    /// The size of a reduction for the evidence on it: 0 with no evidence, the dice minimum with 1, rising evenly to the dice maximum
    /// at <see cref="EvidenceForMaximum"/>. Fixed grants and licensed runs have no evidence-based size.
    /// </summary>
    public static int Magnitude(ResearchCandidate candidate, int evidence)
    {
        if (candidate is not (ResearchCandidate.CreditReduction or ResearchCandidate.TimeReduction) || evidence <= 0)
            return 0;

        var maximum = ResearchGrantRules.MaximumAmount(candidate);
        var capped = Math.Min(evidence, EvidenceForMaximum);
        var steps = (capped - 1) * (maximum - 1) / (double)(EvidenceForMaximum - 1);
        return 1 + (int)Math.Round(steps, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// The winner of an experiment and its bonus. A fixed level's winner is its own grant, and so is a random level's whose
    /// licensed-run roll came up. Otherwise the winner is the candidate with the most evidence, a tie going to one of the tied
    /// candidates by today's odds; with no evidence on either candidate the winner is <see cref="ResearchCandidate.None"/>.
    /// </summary>
    public static (ResearchCandidate Winner, int Magnitude) Resolve(
        int level,
        int licensedRunsGrant,
        IReadOnlyDictionary<ResearchCandidate, int> evidence,
        IMinigameRandom random)
    {
        var candidates = ResearchGrantRules.CandidatesFor(level, licensedRunsGrant);
        if (candidates.Count == 1)
            return (ResearchCandidate.FixedGrant, 0);

        var best = candidates.Max(candidate => EvidenceOf(evidence, candidate));
        if (best <= 0)
            return (ResearchCandidate.None, 0);

        var leaders = candidates.Where(candidate => EvidenceOf(evidence, candidate) == best).ToList();
        var winner = leaders.Count == 1 ? leaders[0] : PickByWeight(leaders, random);
        return (winner, Magnitude(winner, best));
    }

    /// <summary>The candidates sharing the most evidence where evidence decides; empty when there is none.</summary>
    public static IReadOnlyList<ResearchCandidate> Leaders(
        int level,
        int licensedRunsGrant,
        IReadOnlyDictionary<ResearchCandidate, int> evidence)
    {
        var candidates = ResearchGrantRules.CandidatesFor(level, licensedRunsGrant);
        var best = candidates.Max(candidate => EvidenceOf(evidence, candidate));
        return best <= 0
            ? Array.Empty<ResearchCandidate>()
            : candidates.Where(candidate => EvidenceOf(evidence, candidate) == best).ToList();
    }

    private static int EvidenceOf(IReadOnlyDictionary<ResearchCandidate, int> evidence, ResearchCandidate candidate) =>
        evidence != null && evidence.TryGetValue(candidate, out var value) ? value : 0;

    private static ResearchCandidate PickByWeight(IReadOnlyList<ResearchCandidate> tied, IMinigameRandom random)
    {
        var total = tied.Sum(candidate => TieBreakWeights[candidate]);
        var roll = random.Next(0, total);
        var cumulative = 0;
        foreach (var candidate in tied)
        {
            cumulative += TieBreakWeights[candidate];
            if (roll < cumulative)
                return candidate;
        }

        return tied[^1];
    }
}
