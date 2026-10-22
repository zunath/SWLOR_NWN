using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// How an experiment's evidence becomes an outcome: which candidate wins a random level and how large its bonus is. A bonus never
/// exceeds today's dice (licensed runs 1-3, reductions 1-10 percent), and no evidence on any candidate means no outcome, so the job
/// falls back to today's weighted roll when it completes.
/// </summary>
public static class ResearchOutcomeRules
{
    /// <summary>The evidence at which a candidate pays today's dice maximum. One piece of evidence pays the dice minimum.</summary>
    public const int EvidenceForMaximum = 6;

    // How often today's roll picks each candidate: licensed runs 300, credit reduction 400, time reduction 400. Ties use the same odds.
    private static readonly IReadOnlyDictionary<ResearchCandidate, int> TieBreakWeights = new Dictionary<ResearchCandidate, int>
    {
        [ResearchCandidate.LicensedRuns] = 300,
        [ResearchCandidate.CreditReduction] = 400,
        [ResearchCandidate.TimeReduction] = 400
    };

    /// <summary>
    /// The size of a candidate's bonus for the evidence on it: 0 with no evidence, the dice minimum with 1, rising evenly to the dice
    /// maximum at <see cref="EvidenceForMaximum"/>. Fixed grants have no size.
    /// </summary>
    public static int Magnitude(ResearchCandidate candidate, int evidence)
    {
        var maximum = ResearchGrantRules.MaximumAmount(candidate);
        if (maximum <= 0 || evidence <= 0)
            return 0;

        var capped = Math.Min(evidence, EvidenceForMaximum);
        var steps = (capped - 1) * (maximum - 1) / (double)(EvidenceForMaximum - 1);
        return 1 + (int)Math.Round(steps, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// The winner of an experiment and its bonus. A fixed level's winner is its own grant. A random level's winner is the candidate
    /// with the most evidence, a tie going to one of the tied candidates by today's odds; with no evidence on any candidate the
    /// winner is <see cref="ResearchCandidate.None"/>.
    /// </summary>
    public static (ResearchCandidate Winner, int Magnitude) Resolve(
        int level,
        IReadOnlyDictionary<ResearchCandidate, int> evidence,
        IMinigameRandom random)
    {
        if (!ResearchGrantRules.IsRandomLevel(level))
            return (ResearchCandidate.FixedGrant, 0);

        var candidates = ResearchGrantRules.CandidatesFor(level);
        var best = candidates.Max(candidate => EvidenceOf(evidence, candidate));
        if (best <= 0)
            return (ResearchCandidate.None, 0);

        var leaders = candidates.Where(candidate => EvidenceOf(evidence, candidate) == best).ToList();
        var winner = leaders.Count == 1 ? leaders[0] : PickByWeight(leaders, random);
        return (winner, Magnitude(winner, best));
    }

    /// <summary>The candidates sharing the most evidence at a random level; empty when there is none.</summary>
    public static IReadOnlyList<ResearchCandidate> Leaders(int level, IReadOnlyDictionary<ResearchCandidate, int> evidence)
    {
        var candidates = ResearchGrantRules.CandidatesFor(level);
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
